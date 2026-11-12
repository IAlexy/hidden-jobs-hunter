using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HiddenJobsHunter.Api.Models;

namespace HiddenJobsHunter.Api.Services;

public class AiMatcherService : IAiMatcherService
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private static readonly Uri OpenAiChatCompletionsUri = new("https://api.openai.com/v1/chat/completions");

    private readonly HttpClient _httpClient;

    public AiMatcherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HiddenJobsHunter/1.0");
    }

    public Task<JobMatch> EvaluateMatchAsync(JobOffer job, UserProfile profile, AppSettings settings, CancellationToken ct = default)
        => MatchJobAsync(job, profile, settings, ct);

    public async Task<JobMatch> MatchJobAsync(JobOffer job, UserProfile profile, AppSettings settings, CancellationToken ct = default)
    {
        // Try LLM if configured
        if (settings.AiProvider == "Gemini" && !string.IsNullOrWhiteSpace(settings.GeminiApiKey))
        {
            try
            {
                var match = await MatchWithGeminiAsync(job, profile, settings, ct);
                if (match != null) return match;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiMatcherService] Gemini call failed, falling back to heuristic: {ex.Message}");
            }
        }
        else if (settings.AiProvider == "OpenAI" && !string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            try
            {
                var match = await MatchWithOpenAiAsync(job, profile, settings, ct);
                if (match != null) return match;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiMatcherService] OpenAI call failed, falling back to heuristic: {ex.Message}");
            }
        }
        else if (settings.AiProvider == "Ollama")
        {
            try
            {
                var match = await MatchWithOllamaAsync(job, profile, settings, ct);
                if (match != null) return match;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiMatcherService] Ollama call failed, falling back to heuristic: {ex.Message}");
            }
        }

        // Smart Heuristic Fallback
        return MatchWithHeuristic(job, profile);
    }

    public async Task<string> GeneratePitchAsync(JobOffer job, UserProfile profile, AppSettings settings, string? customInstructions = null, CancellationToken ct = default)
    {
        if (settings.AiProvider == "Gemini" && !string.IsNullOrWhiteSpace(settings.GeminiApiKey))
        {
            try
            {
                var prompt = BuildPitchPrompt(job, profile, customInstructions);
                var response = await CallGeminiAsync(prompt, settings.GeminiApiKey, settings.GeminiModel, ct);
                if (!string.IsNullOrWhiteSpace(response)) return response.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiMatcherService] Gemini pitch generation failed: {ex.Message}");
            }
        }
        else if (settings.AiProvider == "OpenAI" && !string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            try
            {
                var prompt = BuildPitchPrompt(job, profile, customInstructions);
                var response = await CallOpenAiAsync(prompt, settings.OpenAiApiKey, settings.OpenAiModel, ct);
                if (!string.IsNullOrWhiteSpace(response)) return response.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiMatcherService] OpenAI pitch generation failed: {ex.Message}");
            }
        }
        else if (settings.AiProvider == "Ollama")
        {
            try
            {
                var prompt = BuildPitchPrompt(job, profile, customInstructions);
                var response = await CallOllamaTextAsync(prompt, settings.OllamaEndpoint, settings.OllamaModel, ct);
                if (!string.IsNullOrWhiteSpace(response)) return response.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiMatcherService] Ollama pitch generation failed: {ex.Message}");
            }
        }

        // Heuristic Pitch Generator
        return GenerateHeuristicPitch(job, profile, customInstructions);
    }

    public Task<UserProfile> ParseResumeAsync(string cvText, string profileName, AppSettings settings, CancellationToken ct = default)
        => ParseCvToProfileAsync(cvText, profileName, settings, ct);

    public async Task<UserProfile> ParseCvToProfileAsync(string cvText, string profileName, AppSettings settings, CancellationToken ct = default)
    {
        var profile = new UserProfile
        {
            Name = profileName,
            ResumeText = cvText,
            UpdatedAt = DateTime.UtcNow
        };

        var prompt = BuildResumeParsingPrompt(cvText);

        if (settings.AiProvider == "Gemini" && !string.IsNullOrWhiteSpace(settings.GeminiApiKey))
        {
            try
            {
                var result = await CallGeminiAsync(prompt, settings.GeminiApiKey, settings.GeminiModel, ct);
                if (TryApplyParsedProfileJson(result, profile)) return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiMatcherService] Gemini CV parse failed: {ex.Message}");
            }
        }
        else if (settings.AiProvider == "OpenAI" && !string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            try
            {
                var result = await CallOpenAiAsync(prompt, settings.OpenAiApiKey, settings.OpenAiModel, ct);
                if (TryApplyParsedProfileJson(result, profile)) return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiMatcherService] OpenAI CV parse failed: {ex.Message}");
            }
        }
        else if (settings.AiProvider == "Ollama")
        {
            try
            {
                var result = await CallOllamaTextAsync(prompt, settings.OllamaEndpoint, settings.OllamaModel, ct, jsonFormat: true);
                if (TryApplyParsedProfileJson(result, profile)) return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiMatcherService] Ollama CV parse failed: {ex.Message}");
            }
        }

        // Heuristic extraction
        ExtractHeuristicProfile(cvText, profile);
        return profile;
    }

    private static string BuildResumeParsingPrompt(string cvText)
    {
        return $@"Tu es un expert en recrutement tech. Analyse le CV / profil ci-dessous et retourne UNIQUEMENT un objet JSON avec ces clés :
{{
  ""title"": ""Titre professionnel (ex: Senior Fullstack Engineer)"",
  ""yearsOfExperience"": 5,
  ""primarySkills"": ""Compétences clés séparées par des virgules"",
  ""secondarySkills"": ""Compétences secondaires séparées par des virgules"",
  ""languages"": ""Langues parlées"",
  ""preferredContracts"": ""CDI,Freelance/Contractor"",
  ""remoteScopePreference"": ""Worldwide,Europe,France"",
  ""expectedSalaryOrTjm"": ""Fourchette estimée""
}}

CV :
{cvText}";
    }

    private static bool TryApplyParsedProfileJson(string rawResponse, UserProfile profile)
    {
        var jsonMatch = ExtractJsonBlock(rawResponse);
        if (string.IsNullOrEmpty(jsonMatch)) return false;

        using var doc = JsonDocument.Parse(jsonMatch);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return false;

        if (root.TryGetProperty("title", out var t)) profile.Title = t.GetString() ?? profile.Title;
        if (root.TryGetProperty("yearsOfExperience", out var y) && y.ValueKind == JsonValueKind.Number) profile.YearsOfExperience = y.GetInt32();
        if (root.TryGetProperty("primarySkills", out var ps)) profile.PrimarySkills = ps.GetString() ?? profile.PrimarySkills;
        if (root.TryGetProperty("secondarySkills", out var ss)) profile.SecondarySkills = ss.GetString() ?? profile.SecondarySkills;
        if (root.TryGetProperty("languages", out var l)) profile.Languages = l.GetString() ?? profile.Languages;
        if (root.TryGetProperty("preferredContracts", out var pc)) profile.PreferredContracts = pc.GetString() ?? profile.PreferredContracts;
        if (root.TryGetProperty("remoteScopePreference", out var rsp)) profile.RemoteScopePreference = rsp.GetString() ?? profile.RemoteScopePreference;
        if (root.TryGetProperty("expectedSalaryOrTjm", out var s)) profile.ExpectedSalaryOrTjm = s.GetString();
        return true;
    }

    private static JobMatch MatchWithHeuristic(JobOffer job, UserProfile profile)
    {
        var jobFullText = $"{job.Title} {job.Company} {job.Description} {job.Tags} {job.Location}".ToLowerInvariant();

        // 1. Tech Stack Fit
        var profileSkills = (profile.PrimarySkills + "," + profile.SecondarySkills)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .Distinct()
            .ToList();

        var matchedSkills = new List<string>();
        var missingSkills = new List<string>();

        foreach (var skill in profileSkills)
        {
            if (skill.Length < 2) continue;
            var pattern = $@"(?<!\w){Regex.Escape(skill)}(?!\w)";
            if (Regex.IsMatch(jobFullText, pattern, RegexOptions.IgnoreCase, RegexTimeout))
            {
                matchedSkills.Add(skill);
            }
        }

        // Detect skills mentioned in job tags/description that are not in profile
        var jobSkillCandidates = new[]
        {
            ".net", "c#", "react", "typescript", "python", "go", "golang", "rust", "java", "kotlin",
            "kubernetes", "aws", "gcp", "azure", "docker", "postgresql", "graphql", "vue", "angular"
        };
        foreach (var candidate in jobSkillCandidates)
        {
            var pattern = $@"(?<!\w){Regex.Escape(candidate)}(?!\w)";
            if (Regex.IsMatch(jobFullText, pattern, RegexOptions.IgnoreCase, RegexTimeout) &&
                !profileSkills.Any(ps => ps.Contains(candidate, StringComparison.OrdinalIgnoreCase)))
            {
                missingSkills.Add($"Compétence demandée non listée dans le profil : {candidate}");
            }
        }

        // Check common stack markers
        bool jobIsDotNet = jobFullText.Contains(".net") || jobFullText.Contains("c#") || jobFullText.Contains("asp.net");
        bool jobIsReact = jobFullText.Contains("react") || jobFullText.Contains("typescript") || jobFullText.Contains("frontend");
        bool jobIsFullstack = jobFullText.Contains("fullstack") || jobFullText.Contains("full stack") || (jobIsDotNet && jobIsReact);

        bool profileHasDotNet = profileSkills.Any(s => s.Contains(".net") || s.Contains("c#"));
        bool profileHasReact = profileSkills.Any(s => s.Contains("react") || s.Contains("typescript"));

        int techScore = 40;
        if (matchedSkills.Count > 0)
        {
            techScore = Math.Min(95, 45 + (matchedSkills.Count * 12));
        }
        if (jobIsDotNet && profileHasDotNet) techScore += 15;
        if (jobIsReact && profileHasReact) techScore += 15;
        if (jobIsFullstack && profileHasDotNet && profileHasReact) techScore += 5;
        techScore = Math.Clamp(techScore, 20, 98);

        // 2. Remote & Administrative Fit from France
        int remoteScore;
        string adminFeasibility;

        bool isWorldwide = job.RemoteScope.Equals("Worldwide", StringComparison.OrdinalIgnoreCase) ||
                           jobFullText.Contains("worldwide") || jobFullText.Contains("anywhere");
        bool isEurope = job.RemoteScope.Equals("Europe", StringComparison.OrdinalIgnoreCase) ||
                        jobFullText.Contains("europe") || jobFullText.Contains("emea") || jobFullText.Contains("france");
        bool isUsOnly = !job.IsFranceCompatible ||
                        job.RemoteScope.Equals("US-Only", StringComparison.OrdinalIgnoreCase) ||
                        jobFullText.Contains("us only") || jobFullText.Contains("united states only");

        if (isUsOnly)
        {
            remoteScore = 25;
            adminFeasibility = "Offre avec forte restriction géographique US/Canada. Possible uniquement si l'entreprise accepte une dispense de visa via contrat B2B freelance.";
        }
        else if (isEurope || jobFullText.Contains("france"))
        {
            remoteScore = 95;
            adminFeasibility = "Excellente compatibilité : fuseau horaire CET / Europe. Éligible en CDI (si entité en France ou via portage/Deel/Remote) ou en Freelance B2B.";
        }
        else if (isWorldwide)
        {
            remoteScore = 90;
            adminFeasibility = "Poste ouvert à l'international / Worldwide. Idéal pour travailler depuis la France en contrat B2B (facturation auto-entrepreneur / société) ou via EOR.";
        }
        else
        {
            remoteScore = 75;
            adminFeasibility = "Poste remote. À valider lors du premier contact : ouverture aux résidents fiscaux français en B2B Contractor ou EOR.";
        }

        // 3. Overall Match Score
        int overallScore = (int)Math.Round((techScore * 0.65) + (remoteScore * 0.35));

        // Strengths & gaps
        var strengths = new List<string>();
        if (matchedSkills.Count > 0)
        {
            strengths.Add($"Alignement technique avéré sur : {string.Join(", ", matchedSkills.Take(5))}");
        }
        if (profile.YearsOfExperience >= 4)
        {
            strengths.Add($"Expérience solide ({profile.YearsOfExperience} ans) idéale pour l'autonomie requise en full remote");
        }
        if (isWorldwide || isEurope)
        {
            strengths.Add("Localisation en France compatible avec la politique remote de l'entreprise");
        }
        if (job.DirectContact != null)
        {
            strengths.Add($"Contact direct identifié ({job.DirectContact}) permettant de bypasser les ATS classiques");
        }

        if (matchedSkills.Count < 2)
        {
            missingSkills.Add("Certaines technos spécifiques demandées ne figurent pas explicitement dans votre profil principal");
        }
        if (jobFullText.Contains("senior") && profile.YearsOfExperience < 5)
        {
            missingSkills.Add("Poste ciblé Senior : mettre en avant l'impact produit et l'autonomie passée");
        }

        return new JobMatch
        {
            JobOfferId = job.Id,
            UserProfileId = profile.Id,
            MatchScore = overallScore,
            TechStackFit = techScore,
            RemoteEligibilityFit = remoteScore,
            Summary = $"Match à {overallScore}% basé sur votre profil ({profile.Title}) et les critères de l'offre.",
            StrengthsJson = JsonSerializer.Serialize(strengths),
            MissingSkillsJson = JsonSerializer.Serialize(missingSkills),
            AdministrativeFeasibility = adminFeasibility,
            TailoredPitch = GenerateHeuristicPitch(job, profile, null),
            AiProviderUsed = "Heuristic",
            AnalyzedAt = DateTime.UtcNow
        };
    }

    private static string GenerateHeuristicPitch(JobOffer job, UserProfile profile, string? customInstructions)
    {
        bool forceFrench = profile.PreferredPitchLanguage.Equals("French", StringComparison.OrdinalIgnoreCase) ||
                           (customInstructions?.Contains("français", StringComparison.OrdinalIgnoreCase) == true);
        bool forceEnglish = profile.PreferredPitchLanguage.Equals("English", StringComparison.OrdinalIgnoreCase) ||
                            (customInstructions?.Contains("english", StringComparison.OrdinalIgnoreCase) == true) ||
                            (customInstructions?.Contains("anglais", StringComparison.OrdinalIgnoreCase) == true);

        bool isFrenchDescription = job.Description.Contains(" nous ", StringComparison.OrdinalIgnoreCase) ||
                                   job.Description.Contains(" vous ", StringComparison.OrdinalIgnoreCase) ||
                                   job.Description.Contains("poste", StringComparison.OrdinalIgnoreCase) ||
                                   job.Description.Contains("équipe", StringComparison.OrdinalIgnoreCase);

        bool isEnglish = forceEnglish || (!forceFrench && !isFrenchDescription);

        var skills = profile.PrimarySkills.Split(',', StringSplitOptions.TrimEntries).Take(4).ToList();
        var skillsStr = string.Join(", ", skills);
        var extraNote = string.IsNullOrWhiteSpace(customInstructions) ? string.Empty : $"\n\nNote: {customInstructions.Trim()}";

        if (isEnglish)
        {
            return $@"Hi {job.Company} team,

I came across your opening for the {job.Title} role and was immediately excited by what you are building.

With ~{profile.YearsOfExperience} years of experience as a Fullstack Software Engineer specializing in {skillsStr}, I have a proven track record of architecting scalable backend services and responsive user experiences in fully distributed, autonomous environments.

Key highlights regarding your requirements:
• Stack alignment: deep hands-on expertise in {skillsStr}.
• Remote autonomy: fully set up for 100% remote collaboration from France (CET timezone, bilingual English/French).
• Administrative flexibility: fully eligible and ready to collaborate either via direct B2B contractor invoicing or through an EOR platform (such as Deel / Remote.com).{extraNote}

I would love to have a brief 15-minute chat to discuss how I can contribute to {job.Company}'s goals this quarter.

Best regards,
{profile.Name}
Fullstack Software Engineer";
        }

        return $@"Bonjour l'équipe {job.Company},

J'ai repéré votre opportunité pour le poste de {job.Title} et le challenge technique m'a tout de suite interpellé.

Avec près de {profile.YearsOfExperience} ans d'expérience en tant qu'ingénieur logiciel Fullstack ({skillsStr}), j'ai développé une solide expertise dans la conception d'architectures résilientes et d'interfaces fluides au sein d'équipes agiles 100% distribuées.

Ce que je peux apporter immédiatement :
• Maîtrise technique : expérience approfondie sur {skillsStr}.
• Travail à distance éprouvé : 100% autonome en full remote depuis la France, bilingue anglais/français.
• Souplesse contractuelle : ouvert en CDI (direct ou portage/EOR) ou en prestation freelance / B2B selon ce qui vous arrange.{extraNote}

Seriez-vous ouvert à un rapide échange de 15 minutes pour faire le point sur vos enjeux actuels ?

Bien cordialement,
{profile.Name}";
    }

    private async Task<JobMatch?> MatchWithGeminiAsync(JobOffer job, UserProfile profile, AppSettings settings, CancellationToken ct)
    {
        var prompt = BuildMatchingPrompt(job, profile);
        var responseText = await CallGeminiAsync(prompt, settings.GeminiApiKey!, settings.GeminiModel, ct);
        return ParseLlmMatchResponse(responseText, job.Id, profile.Id, "Gemini");
    }

    private async Task<JobMatch?> MatchWithOpenAiAsync(JobOffer job, UserProfile profile, AppSettings settings, CancellationToken ct)
    {
        var prompt = BuildMatchingPrompt(job, profile);
        var responseText = await CallOpenAiAsync(prompt, settings.OpenAiApiKey!, settings.OpenAiModel, ct);
        return ParseLlmMatchResponse(responseText, job.Id, profile.Id, "OpenAI");
    }

    private async Task<JobMatch?> MatchWithOllamaAsync(JobOffer job, UserProfile profile, AppSettings settings, CancellationToken ct)
    {
        var prompt = BuildMatchingPrompt(job, profile);
        var responseText = await CallOllamaTextAsync(prompt, settings.OllamaEndpoint, settings.OllamaModel, ct, jsonFormat: true);
        if (string.IsNullOrWhiteSpace(responseText)) return null;
        return ParseLlmMatchResponse(responseText, job.Id, profile.Id, "Ollama");
    }

    private async Task<string> CallOllamaTextAsync(string prompt, string ollamaEndpoint, string ollamaModel, CancellationToken ct, bool jsonFormat = false)
    {
        var endpoint = $"{ollamaEndpoint.TrimEnd('/')}/api/generate";
        object payload = jsonFormat
            ? new { model = ollamaModel, prompt, stream = false, format = "json" }
            : new { model = ollamaModel, prompt, stream = false };

        using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (json.TryGetProperty("response", out var respProp))
        {
            return respProp.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string BuildMatchingPrompt(JobOffer job, UserProfile profile)
    {
        return $@"Tu es un expert RH tech et coach pour ingénieurs logiciels cherchant des offres 'invisibles' en full remote mondial depuis la France.
Analyse le profil du candidat par rapport à l'offre d'emploi ci-dessous.

PROFIL CANDIDAT :
- Nom : {profile.Name}
- Titre : {profile.Title}
- Années d'expérience : {profile.YearsOfExperience} ans
- Compétences : {profile.PrimarySkills} | {profile.SecondarySkills}
- Langues : {profile.Languages}
- Contrats acceptés : {profile.PreferredContracts} (Eligible B2B Contractor: {profile.CanWorkAsContractorB2B})
- Contrainte géographique : Basé en France, disponible fuseau horaire flexible / CET.
- CV complet :
{profile.ResumeText}

OFFRE D'EMPLOI :
- Titre : {job.Title}
- Entreprise : {job.Company}
- Source : {job.Source}
- Localisation / Remote : {job.Location} (Scope : {job.RemoteScope})
- Contact direct : {job.DirectContact ?? "Non précisé"}
- Tags : {job.Tags}
- Description :
{job.Description}

CONSIGNE :
Retourne STRICTEMENT un objet JSON (sans markdown entourant) avec la structure suivante :
{{
  ""matchScore"": 85,
  ""techStackFit"": 90,
  ""remoteEligibilityFit"": 80,
  ""summary"": ""Court résumé en français de l'adéquation"",
  ""strengths"": [""Point fort 1"", ""Point fort 2"", ""Point fort 3""],
  ""missingSkills"": [""Point de vigilance ou compétence manquante 1""],
  ""administrativeFeasibility"": ""Analyse claire de la faisabilité légale/administrative depuis la France (B2B, EOR type Deel, CDI)"",
  ""tailoredPitch"": ""Message d'accroche percutant et professionnel prêt à envoyer au recruteur / CTO (dans la langue de l'annonce)""
}}";
    }

    private static string BuildPitchPrompt(JobOffer job, UserProfile profile, string? customNotes)
    {
        return $@"Rédige un message de contact direct (cold outreach pitch / email) personnalisé, percutant et courtois pour ce poste :
Entreprise : {job.Company}
Poste : {job.Title}
Contact : {job.DirectContact ?? "Hiring Team"}
Description du poste : {job.Description}

Candidat :
Nom : {profile.Name}
Titre : {profile.Title} ({profile.YearsOfExperience} ans d'expérience)
Compétences : {profile.PrimarySkills}
Spécificité : Full remote depuis la France, bilingue anglais, flexible en CDI / EOR ou Freelance B2B.
{(string.IsNullOrEmpty(customNotes) ? "" : $"Instructions supplémentaires : {customNotes}")}

Règles :
- Écris dans la langue de l'offre (anglais si l'offre est en anglais, français si en français).
- Sois concis, direct et convaincant, sans formules pompeuses.
- Met en avant la valeur ajoutée immédiate et propose un court échange.";
    }

    private async Task<string> CallGeminiAsync(string prompt, string apiKey, string model, CancellationToken ct)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var text = json.GetProperty("candidates")[0]
                       .GetProperty("content")
                       .GetProperty("parts")[0]
                       .GetProperty("text")
                       .GetString();

        return text ?? string.Empty;
    }

    private async Task<string> CallOpenAiAsync(string prompt, string apiKey, string model, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, OpenAiChatCompletionsUri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.3
        };

        request.Content = JsonContent.Create(payload);
        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private static JobMatch? ParseLlmMatchResponse(string responseText, int jobId, int profileId, string provider)
    {
        try
        {
            var jsonStr = ExtractJsonBlock(responseText);
            if (string.IsNullOrEmpty(jsonStr)) return null;

            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            int matchScore = root.TryGetProperty("matchScore", out var ms) ? ms.GetInt32() : 75;
            int techFit = root.TryGetProperty("techStackFit", out var tf) ? tf.GetInt32() : 75;
            int remoteFit = root.TryGetProperty("remoteEligibilityFit", out var rf) ? rf.GetInt32() : 75;
            string summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";
            string adminFeasibility = root.TryGetProperty("administrativeFeasibility", out var af) ? af.GetString() ?? "" : "";
            string pitch = root.TryGetProperty("tailoredPitch", out var p) ? p.GetString() ?? "" : "";

            var strengths = new List<string>();
            if (root.TryGetProperty("strengths", out var stArray) && stArray.ValueKind == JsonValueKind.Array)
            {
                strengths = stArray.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            }

            var missing = new List<string>();
            if (root.TryGetProperty("missingSkills", out var msArray) && msArray.ValueKind == JsonValueKind.Array)
            {
                missing = msArray.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            }

            return new JobMatch
            {
                JobOfferId = jobId,
                UserProfileId = profileId,
                MatchScore = Math.Clamp(matchScore, 0, 100),
                TechStackFit = Math.Clamp(techFit, 0, 100),
                RemoteEligibilityFit = Math.Clamp(remoteFit, 0, 100),
                Summary = summary,
                StrengthsJson = JsonSerializer.Serialize(strengths),
                MissingSkillsJson = JsonSerializer.Serialize(missing),
                AdministrativeFeasibility = adminFeasibility,
                TailoredPitch = pitch,
                AiProviderUsed = provider,
                AnalyzedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AiMatcherService] Error parsing LLM response: {ex.Message}");
            return null;
        }
    }

    private static string ExtractJsonBlock(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }
        return text.Trim();
    }

    private static void ExtractHeuristicProfile(string cvText, UserProfile profile)
    {
        if (string.IsNullOrWhiteSpace(cvText)) return;

        var textLower = cvText.ToLowerInvariant();

        // Extract years of experience (e.g. "7 ans", "8 years", "6+ years")
        var expMatch = Regex.Match(textLower, @"(\d{1,2})\+?\s*(?:ans|years|yrs)", RegexOptions.IgnoreCase, RegexTimeout);
        if (expMatch.Success && int.TryParse(expMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var years))
        {
            profile.YearsOfExperience = Math.Clamp(years, 0, 40);
        }
        else if (textLower.Contains("senior") || textLower.Contains("lead") || textLower.Contains("principal"))
        {
            profile.YearsOfExperience = 6;
        }

        // Extract title
        if (textLower.Contains("devops") || textLower.Contains("sre"))
        {
            profile.Title = "Senior DevOps / Cloud Engineer";
        }
        else if (textLower.Contains("fullstack") || textLower.Contains("full-stack") || textLower.Contains("full stack"))
        {
            profile.Title = "Senior Fullstack Software Engineer";
        }
        else if (textLower.Contains("backend") || textLower.Contains("back-end"))
        {
            profile.Title = "Backend Software Engineer";
        }
        else if (textLower.Contains("frontend") || textLower.Contains("front-end"))
        {
            profile.Title = "Frontend Software Engineer";
        }

        // Extract primary skills
        var knownPrimary = new[]
        {
            ".NET", "C#", "ASP.NET Core", "React", "TypeScript", "JavaScript", "Node.js", "Python",
            "Go", "Rust", "Java", "Vue", "Angular", "Next.js", "SQL", "PostgreSQL", "SQL Server"
        };
        var detectedPrimary = knownPrimary
            .Where(skill => cvText.Contains(skill, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (detectedPrimary.Count > 0)
        {
            profile.PrimarySkills = string.Join(", ", detectedPrimary);
        }

        // Extract secondary skills
        var knownSecondary = new[]
        {
            "Docker", "Kubernetes", "Azure", "AWS", "GCP", "Redis", "GraphQL", "TailwindCSS", "CI/CD", "GitHub Actions", "Terraform"
        };
        var detectedSecondary = knownSecondary
            .Where(skill => cvText.Contains(skill, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (detectedSecondary.Count > 0)
        {
            profile.SecondarySkills = string.Join(", ", detectedSecondary);
        }

        // Extract languages
        var detectedLangs = new List<string>();
        if (textLower.Contains("français") || textLower.Contains("french")) detectedLangs.Add("Français");
        if (textLower.Contains("anglais") || textLower.Contains("english")) detectedLangs.Add("Anglais");
        if (textLower.Contains("espagnol") || textLower.Contains("spanish")) detectedLangs.Add("Espagnol");
        if (detectedLangs.Count > 0)
        {
            profile.Languages = string.Join(", ", detectedLangs);
        }
    }
}

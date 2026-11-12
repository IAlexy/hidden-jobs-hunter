using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HiddenJobsHunter.Api.Models;
using HiddenJobsHunter.Api.Services;

namespace HiddenJobsHunter.Tests;

public class AiMatcherServiceTests
{
    private static UserProfile CreateSampleProfile() => new()
    {
        Id = 1,
        Name = "Demo Candidate",
        Title = "Senior Fullstack Software Engineer (.NET / React)",
        YearsOfExperience = 6,
        PrimarySkills = ".NET, C#, ASP.NET Core, React, TypeScript, PostgreSQL",
        SecondarySkills = "Docker, Azure, Redis, CI/CD",
        Languages = "Français (natif), Anglais (bilingue)",
        PreferredContracts = "CDI,Freelance/Contractor",
        RemoteScopePreference = "Worldwide,Europe,France",
        CanWorkAsContractorB2B = true,
        PreferredPitchLanguage = "Auto"
    };

    [Fact]
    public async Task EvaluateMatchAsync_Heuristic_StrongFitEurope_ComputesHighScoreAndEligibility()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new AiMatcherService(httpClient);
        var profile = CreateSampleProfile();
        var settings = new AppSettings { AiProvider = "Heuristic" };

        var job = new JobOffer
        {
            Id = 10,
            ExternalId = "hn_10",
            Source = "HackerNews",
            Title = "Senior Fullstack Engineer (.NET 8 / React / TypeScript)",
            Company = "EuroTech",
            Location = "Remote (Europe / CET)",
            RemoteScope = "Europe",
            IsFranceCompatible = true,
            DirectContact = "cto@eurotech.io",
            Description = "Looking for a Senior Fullstack Engineer with C#, ASP.NET Core, React, TypeScript, PostgreSQL, Docker and Kubernetes.",
            Tags = ".NET, C#, React, TypeScript, Docker, Kubernetes"
        };

        var match = await service.EvaluateMatchAsync(job, profile, settings);

        Assert.Equal("Heuristic", match.AiProviderUsed);
        Assert.True(match.MatchScore >= 85);
        Assert.True(match.TechStackFit >= 85);
        Assert.Equal(95, match.RemoteEligibilityFit);
        Assert.Contains("CET / Europe", match.AdministrativeFeasibility, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CDI", match.AdministrativeFeasibility, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Freelance B2B", match.AdministrativeFeasibility, StringComparison.OrdinalIgnoreCase);

        var strengths = JsonSerializer.Deserialize<List<string>>(match.StrengthsJson) ?? new List<string>();
        var missing = JsonSerializer.Deserialize<List<string>>(match.MissingSkillsJson) ?? new List<string>();

        Assert.Contains(strengths, s => s.Contains("Alignement technique", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(strengths, s => s.Contains("cto@eurotech.io", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(missing, m => m.Contains("kubernetes", StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrWhiteSpace(match.Summary));
        Assert.False(string.IsNullOrWhiteSpace(match.TailoredPitch));
    }

    [Fact]
    public async Task EvaluateMatchAsync_Heuristic_WorldwideAndUsOnlyRestrictions_HandlesBothCorrectly()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new AiMatcherService(httpClient);
        var profile = CreateSampleProfile();
        profile.YearsOfExperience = 3;
        var settings = new AppSettings { AiProvider = "Heuristic" };

        var worldwideJob = new JobOffer
        {
            Id = 11,
            ExternalId = "rem_11",
            Source = "Remotive",
            Title = "Senior Backend Engineer",
            Company = "GlobalCorp",
            Location = "Worldwide",
            RemoteScope = "Worldwide",
            IsFranceCompatible = true,
            Description = "Worldwide remote role using Rust and Go.",
            Tags = "Rust, Go"
        };

        var wwMatch = await service.EvaluateMatchAsync(worldwideJob, profile, settings);
        Assert.Equal(90, wwMatch.RemoteEligibilityFit);
        Assert.Contains("EOR", wwMatch.AdministrativeFeasibility, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("B2B", wwMatch.AdministrativeFeasibility, StringComparison.OrdinalIgnoreCase);

        var usOnlyJob = new JobOffer
        {
            Id = 12,
            ExternalId = "hn_12",
            Source = "HackerNews",
            Title = "Software Engineer",
            Company = "USStartup",
            Location = "US Only",
            RemoteScope = "US-Only",
            IsFranceCompatible = false,
            Description = "Remote within the United States only. C# and React.",
            Tags = "C#, React"
        };

        var usMatch = await service.EvaluateMatchAsync(usOnlyJob, profile, settings);
        Assert.Equal(25, usMatch.RemoteEligibilityFit);
        Assert.Contains("restriction géographique US", usMatch.AdministrativeFeasibility, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateMatchAsync_LlmProvidersAndFallback_ReturnsExpectedMatches()
    {
        var geminiJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"{
                                    ""matchScore"": 92,
                                    ""techStackFit"": 95,
                                    ""remoteEligibilityFit"": 88,
                                    ""summary"": ""Excellent match Gemini"",
                                    ""strengths"": [""C#/.NET 8"", ""React 19""],
                                    ""missingSkills"": [""GraphQL""],
                                    ""administrativeFeasibility"": ""Contrat B2B ou EOR Deel"",
                                    ""tailoredPitch"": ""Hello from Gemini""
                                }"
                            }
                        }
                    }
                }
            }
        });

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri?.Host.Contains("googleapis.com", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(geminiJson, Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }));

        var service = new AiMatcherService(httpClient);
        var profile = CreateSampleProfile();
        var job = new JobOffer
        {
            Id = 20,
            ExternalId = "gh_20",
            Source = "CustomAts",
            Title = "Fullstack Engineer",
            Company = "Acme",
            Description = "C# and React role"
        };

        // 1. Gemini success
        var geminiSettings = new AppSettings { AiProvider = "Gemini", GeminiApiKey = "test-gemini-key" };
        var geminiMatch = await service.EvaluateMatchAsync(job, profile, geminiSettings);
        Assert.Equal("Gemini", geminiMatch.AiProviderUsed);
        Assert.Equal(92, geminiMatch.MatchScore);
        Assert.Equal("Excellent match Gemini", geminiMatch.Summary);

        // 2. OpenAI failure -> fallback to Heuristic
        var openAiSettings = new AppSettings { AiProvider = "OpenAI", OpenAiApiKey = "test-openai-key" };
        var fallbackMatch = await service.EvaluateMatchAsync(job, profile, openAiSettings);
        Assert.Equal("Heuristic", fallbackMatch.AiProviderUsed);
        Assert.True(fallbackMatch.MatchScore > 0);
    }

    [Fact]
    public async Task GeneratePitchAsync_HeuristicAndMockedLlm_SupportsFrenchAndEnglish()
    {
        var openAiPitchResponse = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Custom LLM generated pitch for Acme."
                    }
                }
            }
        });

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(openAiPitchResponse, Encoding.UTF8, "application/json")
            }));

        var service = new AiMatcherService(httpClient);
        var profile = CreateSampleProfile();
        var heuristicSettings = new AppSettings { AiProvider = "Heuristic" };

        var englishJob = new JobOffer
        {
            Id = 30,
            ExternalId = "hn_30",
            Source = "HackerNews",
            Title = "Staff Engineer",
            Company = "LinearClone",
            Description = "We are building the next generation issue tracker in React and TypeScript."
        };

        var enPitch = await service.GeneratePitchAsync(englishJob, profile, heuristicSettings, "Mention micro-services");
        Assert.Contains("Hi LinearClone team", enPitch, StringComparison.Ordinal);
        Assert.Contains("B2B contractor", enPitch, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Mention micro-services", enPitch, StringComparison.Ordinal);

        var frenchJob = new JobOffer
        {
            Id = 31,
            ExternalId = "hn_31",
            Source = "HackerNews",
            Title = "Ingénieur Fullstack .NET",
            Company = " FrenchTech ",
            Description = "Nous recherchons un développeur pour rejoindre notre équipe produit en télétravail."
        };

        var frPitch = await service.GeneratePitchAsync(frenchJob, profile, heuristicSettings);
        Assert.Contains("Bonjour l'équipe", frPitch, StringComparison.Ordinal);
        Assert.Contains("CDI", frPitch, StringComparison.Ordinal);

        // Mocked OpenAI LLM pitch
        var openAiSettings = new AppSettings { AiProvider = "OpenAI", OpenAiApiKey = "sk-test-123456" };
        var llmPitch = await service.GeneratePitchAsync(englishJob, profile, openAiSettings);
        Assert.Equal("Custom LLM generated pitch for Acme.", llmPitch);
    }

    [Fact]
    public async Task ParseResumeAsync_HeuristicAndMockedLlm_ExtractsSkillsExperienceTitleAndLanguages()
    {
        var ollamaResponse = JsonSerializer.Serialize(new
        {
            response = @"{
                ""title"": ""Principal Cloud Architect"",
                ""yearsOfExperience"": 9,
                ""primarySkills"": "".NET, C#, Azure, Kubernetes"",
                ""secondarySkills"": ""Terraform, Docker"",
                ""languages"": ""Français, Anglais"",
                ""preferredContracts"": ""Freelance/Contractor"",
                ""remoteScopePreference"": ""Worldwide"",
                ""expectedSalaryOrTjm"": ""750€ TJM""
            }"
        });

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath.Contains("/api/generate", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ollamaResponse, Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }));

        var service = new AiMatcherService(httpClient);

        // 1. Heuristic resume parsing
        var rawResume = @"
            Senior Fullstack Developer with 8 years of experience.
            Stack: .NET, C#, ASP.NET Core, React, TypeScript, PostgreSQL, Docker, Azure, Redis.
            Languages: Français (native), English (fluent).
        ";
        var heuristicProfile = await service.ParseResumeAsync(rawResume, "Heuristic Profile", new AppSettings { AiProvider = "Heuristic" });
        Assert.Equal("Heuristic Profile", heuristicProfile.Name);
        Assert.Equal(8, heuristicProfile.YearsOfExperience);
        Assert.Contains("Fullstack", heuristicProfile.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".NET", heuristicProfile.PrimarySkills, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("React", heuristicProfile.PrimarySkills, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Docker", heuristicProfile.SecondarySkills, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Français", heuristicProfile.Languages, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Anglais", heuristicProfile.Languages, StringComparison.OrdinalIgnoreCase);

        // 2. Mocked Ollama LLM resume parsing
        var ollamaSettings = new AppSettings { AiProvider = "Ollama", OllamaEndpoint = "http://localhost:11434", OllamaModel = "llama3" };
        var llmProfile = await service.ParseResumeAsync(rawResume, "Ollama Profile", ollamaSettings);
        Assert.Equal("Principal Cloud Architect", llmProfile.Title);
        Assert.Equal(9, llmProfile.YearsOfExperience);
        Assert.Equal("750€ TJM", llmProfile.ExpectedSalaryOrTjm);
    }

    [Fact]
    public async Task EvaluateMatchAsync_MultiDomainAndMultiCountry_ScoresFairlyWithoutDotNetBias()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new AiMatcherService(httpClient);
        var settings = new AppSettings { AiProvider = "Heuristic" };

        // 1. Data Scientist in Canada applying to a US/North-America AI role (with English language preference)
        var dataProfileCanada = new UserProfile
        {
            Id = 2,
            Name = "Dr. Claire Data",
            Title = "Senior Data Scientist & AI Engineer",
            Country = "Canada",
            JobCategory = "data",
            YearsOfExperience = 6,
            PrimarySkills = "Python, PyTorch, LLM, RAG, Pandas, SQL, Machine Learning",
            SecondarySkills = "Snowflake, dbt, Docker, AWS",
            Languages = "English (fluent), Français (native)",
            PreferredPitchLanguage = "English"
        };

        var aiJob = new JobOffer
        {
            Id = 50,
            ExternalId = "hn_50",
            Source = "HackerNews",
            Title = "Senior Data Scientist / AI Engineer (PyTorch & LLM)",
            Company = "NorthStarAI",
            Location = "US / Canada Only",
            RemoteScope = "US-Only",
            IsFranceCompatible = false,
            Description = "Seeking a Senior Data Scientist experienced in Python, PyTorch, LLM, RAG, Pandas, SQL, Snowflake and TensorFlow.",
            Tags = "Python, PyTorch, LLM, Pandas, SQL, TensorFlow"
        };

        var dataMatch = await service.EvaluateMatchAsync(aiJob, dataProfileCanada, settings);
        Assert.True(dataMatch.TechStackFit >= 85);
        Assert.Equal(92, dataMatch.RemoteEligibilityFit);
        Assert.Contains("match based on your profile", dataMatch.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Excellent geographic compatibility (Canada)", dataMatch.AdministrativeFeasibility, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Strong skill alignment on:", dataMatch.StrengthsJson, StringComparison.Ordinal);
        Assert.Contains("Requested skill not listed in profile: tensorflow", dataMatch.MissingSkillsJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Senior Data Scientist & AI Engineer", dataMatch.TailoredPitch, StringComparison.Ordinal);
        Assert.Contains("from Canada", dataMatch.TailoredPitch, StringComparison.Ordinal);
        Assert.DoesNotContain("Fullstack Software Engineer", dataMatch.TailoredPitch, StringComparison.Ordinal);

        // 2. UX/UI Product Designer in Switzerland applying to a European Design role
        var designerProfileSwiss = new UserProfile
        {
            Id = 3,
            Name = "Marc Design",
            Title = "Senior UX/UI Product Designer",
            Country = "Switzerland",
            JobCategory = "design",
            YearsOfExperience = 5,
            PrimarySkills = "Figma, Design Systems, UX Research, Prototyping, Wireframing",
            SecondarySkills = "Framer, Accessibility",
            Languages = "Français, Anglais, Allemand",
            PreferredPitchLanguage = "French"
        };

        var designJob = new JobOffer
        {
            Id = 51,
            ExternalId = "rem_51",
            Source = "Remotive",
            Title = "Lead Product Designer (Figma / Design Systems)",
            Company = "HelvetiaSaaS",
            Location = "Remote (Europe)",
            RemoteScope = "Europe",
            IsFranceCompatible = true,
            Description = "Nous cherchons un Product Designer maîtrisant Figma, Design Systems, UX Research et Prototyping pour notre équipe produit.",
            Tags = "Figma, Design Systems, UX Research"
        };

        var designMatch = await service.EvaluateMatchAsync(designJob, designerProfileSwiss, settings);
        Assert.True(designMatch.TechStackFit >= 85);
        Assert.Equal(95, designMatch.RemoteEligibilityFit);
        Assert.Contains("Switzerland", designMatch.AdministrativeFeasibility, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Senior UX/UI Product Designer", designMatch.TailoredPitch, StringComparison.Ordinal);
        Assert.Contains("depuis Switzerland", designMatch.TailoredPitch, StringComparison.Ordinal);
    }
}

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responder(request));
    }
}

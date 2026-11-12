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
using HtmlAgilityPack;
using HiddenJobsHunter.Api.Models;

namespace HiddenJobsHunter.Api.Providers;

public class CustomAtsProvider : IJobProvider
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private readonly HttpClient _httpClient;

    public string Id => "CustomAts";
    public string Name => "ATS Directs (Ashby, Greenhouse, Lever)";
    public string Description => "Extraction directe des offres depuis les pages carrières d'entreprises cibles (sans passer par LinkedIn).";
    public bool IsEnabledByDefault => false;

    public CustomAtsProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<List<JobOffer>> FetchJobsAsync(int limit = 50, CancellationToken ct = default)
    {
        // Default target companies with strong remote culture
        var targetCompanies = new[]
        {
            ("Automattic", "https://boards-api.greenhouse.io/v1/boards/automattic/jobs?content=true", "Greenhouse"),
            ("GitLab", "https://boards-api.greenhouse.io/v1/boards/gitlab/jobs?content=true", "Greenhouse"),
            ("Buffer", "https://api.lever.co/v0/postings/buffer?mode=json", "Lever"),
        };

        var results = new List<JobOffer>();
        foreach (var (company, url, type) in targetCompanies)
        {
            if (results.Count >= limit) break;
            try
            {
                if (type == "Greenhouse")
                {
                    var ghJobs = await FetchGreenhouseAsync(company, url, ct);
                    results.AddRange(ghJobs);
                }
                else if (type == "Lever")
                {
                    var leverJobs = await FetchLeverAsync(company, url, ct);
                    results.AddRange(leverJobs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CustomAtsProvider] Failed {company}: {ex.Message}");
            }
        }

        return results.Take(limit).ToList();
    }

    public async Task<List<JobOffer>> IngestUrlAsync(string targetUrl, CancellationToken ct = default)
    {
        var results = new List<JobOffer>();

        try
        {
            // Detect Greenhouse
            var ghMatch = Regex.Match(targetUrl, @"(?:boards\.greenhouse\.io|job-boards\.greenhouse\.io|boards-api\.greenhouse\.io/v1/boards)/([^/?#]+)", RegexOptions.IgnoreCase, RegexTimeout);
            if (ghMatch.Success)
            {
                var token = ghMatch.Groups[1].Value;
                var apiUrl = $"https://boards-api.greenhouse.io/v1/boards/{token}/jobs?content=true";
                return await FetchGreenhouseAsync(token, apiUrl, ct);
            }

            // Detect Lever
            var leverMatch = Regex.Match(targetUrl, @"(?:jobs\.lever\.co|api\.lever\.co/v0/postings)/([^/?#]+)", RegexOptions.IgnoreCase, RegexTimeout);
            if (leverMatch.Success)
            {
                var token = leverMatch.Groups[1].Value;
                var apiUrl = $"https://api.lever.co/v0/postings/{token}?mode=json";
                return await FetchLeverAsync(token, apiUrl, ct);
            }

            // Detect Ashby
            var ashbyMatch = Regex.Match(targetUrl, @"(?:jobs\.ashbyhq\.com|api\.ashbyhq\.com/posting-api/job-board)/([^/?#]+)", RegexOptions.IgnoreCase, RegexTimeout);
            if (ashbyMatch.Success)
            {
                var token = ashbyMatch.Groups[1].Value;
                var apiUrl = $"https://api.ashbyhq.com/posting-api/job-board/{token}";
                return await FetchAshbyAsync(token, apiUrl, ct);
            }

            // Generic web scraping fallback
            using var response = await _httpClient.GetAsync(targetUrl, ct);
            if (!response.IsSuccessStatusCode) return results;

            var html = await response.Content.ReadAsStringAsync(ct);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            var pageTitle = titleNode != null ? HtmlEntity.DeEntitize(titleNode.InnerText).Trim() : "Company Careers";

            // Extract job links or headings
            var jobNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'job') or contains(@href, 'career') or contains(@href, 'position')]")
                           ?? doc.DocumentNode.SelectNodes("//h2 | //h3");

            if (jobNodes != null)
            {
                foreach (var node in jobNodes.Take(15))
                {
                    var text = HtmlEntity.DeEntitize(node.InnerText).Trim();
                    if (text.Length > 5 && text.Length < 90 && !text.Contains("Copyright") && !text.Contains("Cookie"))
                    {
                        var link = node.GetAttributeValue("href", targetUrl);
                        if (!link.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            var baseUri = new Uri(targetUrl);
                            link = new Uri(baseUri, link).ToString();
                        }

                        results.Add(new JobOffer
                        {
                            ExternalId = $"custom_{Guid.NewGuid():N}",
                            Source = "CustomAts",
                            Title = text,
                            Company = pageTitle.Split('-', '|', ':')[0].Trim(),
                            Location = "Remote",
                            RemoteScope = "Remote",
                            IsFranceCompatible = true,
                            ContractType = "CDI / Full-time",
                            ApplyUrl = link,
                            Description = $"Poste extrait depuis la page carrière : {text}",
                            Tags = "Fullstack, Software",
                            PublishedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            Status = "Discovered"
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomAtsProvider] Ingest error for {targetUrl}: {ex.Message}");
        }

        return results;
    }

    private async Task<List<JobOffer>> FetchGreenhouseAsync(string company, string apiUrl, CancellationToken ct)
    {
        var list = new List<JobOffer>();
        using var response = await _httpClient.GetAsync(apiUrl, ct);
        if (!response.IsSuccessStatusCode) return list;

        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!doc.TryGetProperty("jobs", out var jobsArray)) return list;

        foreach (var j in jobsArray.EnumerateArray())
        {
            var id = j.GetProperty("id").ValueKind == JsonValueKind.Number
                ? j.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture)
                : j.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
            var title = j.GetProperty("title").GetString() ?? "Software Engineer";
            var url = j.TryGetProperty("absolute_url", out var urlProp) ? urlProp.GetString() ?? "" : "";
            var loc = j.TryGetProperty("location", out var locProp) && locProp.TryGetProperty("name", out var nProp)
                ? nProp.GetString() ?? "Remote" : "Remote";
            var content = j.TryGetProperty("content", out var cProp) ? cProp.GetString() ?? "" : "";
            var cleanDesc = CleanHtml(content);

            var locLower = loc.ToLowerInvariant();
            bool isRemote = locLower.Contains("remote") || cleanDesc.Contains("remote", StringComparison.OrdinalIgnoreCase);
            if (!isRemote) continue;

            bool usOnly = locLower.Contains("us only") || locLower.Contains("usa only");
            bool isWorldwide = locLower.Contains("worldwide") || locLower.Contains("anywhere") || cleanDesc.Contains("worldwide", StringComparison.OrdinalIgnoreCase);

            list.Add(new JobOffer
            {
                ExternalId = $"gh_{company}_{id}",
                Source = "CustomAts (Greenhouse)",
                Title = title,
                Company = company,
                Location = loc,
                RemoteScope = isWorldwide ? "Worldwide" : "Remote",
                IsFranceCompatible = !usOnly,
                ContractType = "CDI / Full-time",
                ApplyUrl = url,
                Description = cleanDesc.Length > 2500 ? cleanDesc[..2500] + "..." : cleanDesc,
                Tags = ExtractTechTags(title + " " + cleanDesc),
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Status = "Discovered"
            });
        }

        return list;
    }

    private async Task<List<JobOffer>> FetchLeverAsync(string company, string apiUrl, CancellationToken ct)
    {
        var list = new List<JobOffer>();
        using var response = await _httpClient.GetAsync(apiUrl, ct);
        if (!response.IsSuccessStatusCode) return list;

        var postings = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (postings.ValueKind != JsonValueKind.Array) return list;

        foreach (var j in postings.EnumerateArray())
        {
            var id = j.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
            var title = j.GetProperty("text").GetString() ?? "Engineer";
            var url = j.TryGetProperty("hostedUrl", out var huProp) ? huProp.GetString() ?? "" : "";
            var desc = j.TryGetProperty("descriptionPlain", out var dpProp) ? dpProp.GetString() ?? "" : "";
            var loc = "Remote";
            if (j.TryGetProperty("categories", out var catProp) && catProp.TryGetProperty("location", out var lProp))
            {
                loc = lProp.GetString() ?? "Remote";
            }

            list.Add(new JobOffer
            {
                ExternalId = $"lever_{company}_{id}",
                Source = "CustomAts (Lever)",
                Title = title,
                Company = company,
                Location = loc,
                RemoteScope = "Remote",
                IsFranceCompatible = !loc.Contains("US Only", StringComparison.OrdinalIgnoreCase),
                ContractType = "CDI / Full-time",
                ApplyUrl = url,
                Description = desc.Length > 2500 ? desc[..2500] + "..." : desc,
                Tags = ExtractTechTags(title + " " + desc),
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Status = "Discovered"
            });
        }

        return list;
    }

    private async Task<List<JobOffer>> FetchAshbyAsync(string company, string apiUrl, CancellationToken ct)
    {
        var list = new List<JobOffer>();
        using var response = await _httpClient.GetAsync(apiUrl, ct);
        if (!response.IsSuccessStatusCode) return list;

        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!doc.TryGetProperty("jobs", out var jobsArray)) return list;

        foreach (var j in jobsArray.EnumerateArray())
        {
            var id = j.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
            var title = j.GetProperty("title").GetString() ?? "Engineer";
            var url = j.TryGetProperty("jobUrl", out var uProp) ? uProp.GetString() ?? "" : "";
            var loc = j.TryGetProperty("location", out var lProp) ? lProp.GetString() ?? "Remote" : "Remote";

            list.Add(new JobOffer
            {
                ExternalId = $"ashby_{company}_{id}",
                Source = "CustomAts (Ashby)",
                Title = title,
                Company = company,
                Location = loc,
                RemoteScope = "Remote",
                IsFranceCompatible = true,
                ContractType = "CDI / Full-time",
                ApplyUrl = url,
                Description = $"Poste Ashby: {title} chez {company}",
                Tags = ExtractTechTags(title),
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Status = "Discovered"
            });
        }

        return list;
    }

    private static string CleanHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return HtmlEntity.DeEntitize(doc.DocumentNode.InnerText).Trim();
    }

    private static string ExtractTechTags(string text)
    {
        var keywords = new[] { ".NET", "C#", "React", "TypeScript", "JavaScript", "ASP.NET", "Node", "Python", "Go", "Rust", "Azure", "AWS", "SQL", "Docker" };
        var matches = keywords.Where(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)).Take(6);
        return string.Join(", ", matches);
    }
}

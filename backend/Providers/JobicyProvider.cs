using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HiddenJobsHunter.Api.Models;

namespace HiddenJobsHunter.Api.Providers;

public class JobicyProvider : IJobProvider
{
    private readonly HttpClient _httpClient;

    public string Id => "Jobicy";
    public string Name => "Jobicy (Remote Tech Worldwide)";
    public string Description => "Flux de jobs tech à l'international, souvent ouverts aux contrats B2B dans le monde.";
    public bool IsEnabledByDefault => true;

    public JobicyProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HiddenJobsHunter/1.0");
    }

    public async Task<List<JobOffer>> FetchJobsAsync(int limit = 50, CancellationToken ct = default)
    {
        var jobs = new List<JobOffer>();

        try
        {
            var url = $"https://jobicy.com/api/v2/remote-jobs?count={limit}&industry=engineering";
            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return jobs;

            var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (!doc.TryGetProperty("jobs", out var jobsArray)) return jobs;

            foreach (var j in jobsArray.EnumerateArray())
            {
                var id = j.GetProperty("id").ValueKind == JsonValueKind.Number
                    ? j.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture)
                    : j.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
                var title = j.GetProperty("jobTitle").GetString() ?? "Software Engineer";
                var company = j.GetProperty("companyName").GetString() ?? "Startup";
                var jobUrl = j.GetProperty("url").GetString() ?? "";
                var geo = j.TryGetProperty("jobGeo", out var geoProp) ? geoProp.GetString() ?? "Anywhere" : "Anywhere";
                var rawDesc = j.TryGetProperty("jobDescription", out var descProp) ? descProp.GetString() ?? "" : "";
                var salaryMin = j.TryGetProperty("annualSalaryMin", out var minProp) ? minProp.ToString() : null;
                var salaryMax = j.TryGetProperty("annualSalaryMax", out var maxProp) ? maxProp.ToString() : null;
                var currency = j.TryGetProperty("salaryCurrency", out var curProp) ? curProp.GetString() : "$";

                string? salary = null;
                if (!string.IsNullOrEmpty(salaryMin) || !string.IsNullOrEmpty(salaryMax))
                {
                    salary = $"{salaryMin} - {salaryMax} {currency}";
                }

                var cleanDesc = CleanHtml(rawDesc);

                var geoLower = geo.ToLowerInvariant();
                bool usOnly = geoLower.Contains("usa only") || geoLower.Contains("us only") || geoLower.Equals("usa") || geoLower.Equals("us");
                bool isWorldwide = geoLower.Contains("anywhere") || geoLower.Contains("worldwide") || geoLower.Contains("global");
                bool isEurope = geoLower.Contains("europe") || geoLower.Contains("uk") || geoLower.Contains("emea") || geoLower.Contains("france");

                string remoteScope = "Remote";
                if (isWorldwide) remoteScope = "Worldwide";
                else if (isEurope) remoteScope = "Europe";
                else if (usOnly) remoteScope = "US-Only";

                bool isFranceCompatible = !usOnly && (isWorldwide || isEurope || !geoLower.Contains("only"));

                DateTime pubDate = DateTime.UtcNow;
                if (j.TryGetProperty("pubDate", out var pdProp) &&
                    DateTime.TryParse(pdProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var pd))
                {
                    pubDate = pd;
                }

                jobs.Add(new JobOffer
                {
                    ExternalId = $"jobicy_{id}",
                    Source = "Jobicy",
                    Title = title,
                    Company = company,
                    Location = geo,
                    RemoteScope = remoteScope,
                    IsFranceCompatible = isFranceCompatible,
                    ContractType = "CDI / Full-time",
                    Salary = salary,
                    ApplyUrl = jobUrl,
                    Description = cleanDesc,
                    Tags = ExtractTags(title + " " + cleanDesc),
                    PublishedAt = pubDate,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Discovered"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JobicyProvider] Error: {ex.Message}");
        }

        return jobs;
    }

    private static string CleanHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return HtmlEntity.DeEntitize(doc.DocumentNode.InnerText).Trim();
    }

    private static string ExtractTags(string text)
    {
        var keywords = new[] { ".NET", "C#", "React", "TypeScript", "JavaScript", "ASP.NET", "Node", "Python", "Go", "Rust", "Azure", "AWS", "PostgreSQL", "Docker", "Fullstack", "Frontend", "Backend" };
        var matches = keywords.Where(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)).Take(6);
        return string.Join(", ", matches);
    }
}

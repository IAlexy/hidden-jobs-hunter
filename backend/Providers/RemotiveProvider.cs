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

public class RemotiveProvider : IJobProvider
{
    private readonly HttpClient _httpClient;

    public string Id => "Remotive";
    public string Name => "Remotive (API Remote Tech)";
    public string Description => "Offres 100% remote en développement logiciel, filtrables mondialement / Europe.";
    public bool IsEnabledByDefault => true;

    public RemotiveProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HiddenJobsHunter/1.0");
    }

    public async Task<List<JobOffer>> FetchJobsAsync(int limit = 50, CancellationToken ct = default)
    {
        var jobs = new List<JobOffer>();

        try
        {
            var url = $"https://remotive.com/api/remote-jobs?category=software-dev&limit={limit}";
            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return jobs;

            var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (!doc.TryGetProperty("jobs", out var jobsArray)) return jobs;

            foreach (var j in jobsArray.EnumerateArray())
            {
                var id = j.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture);
                var title = j.GetProperty("title").GetString() ?? "Software Engineer";
                var company = j.GetProperty("company_name").GetString() ?? "Tech Company";
                var jobUrl = j.GetProperty("url").GetString() ?? "";
                var location = j.TryGetProperty("candidate_required_location", out var locProp) ? locProp.GetString() ?? "Worldwide" : "Worldwide";
                var rawJobType = j.TryGetProperty("job_type", out var jtProp) ? jtProp.GetString() ?? "full_time" : "full_time";
                var salary = j.TryGetProperty("salary", out var salProp) ? salProp.GetString() : null;
                var rawDesc = j.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";

                var cleanDesc = CleanHtml(rawDesc);

                // Analyze tags
                var tagsList = new List<string>();
                if (j.TryGetProperty("tags", out var tProp) && tProp.ValueKind == JsonValueKind.Array)
                {
                    tagsList = tProp.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
                }

                // Analyze remote eligibility for France
                var locLower = location.ToLowerInvariant();
                bool usOnly = locLower.Contains("usa only") || locLower.Contains("us only") || locLower.Contains("only us");
                bool isWorldwide = locLower.Contains("worldwide") || locLower.Contains("anywhere");
                bool isEurope = locLower.Contains("europe") || locLower.Contains("emea") || locLower.Contains("france") || locLower.Contains("uk");

                string remoteScope = "Remote";
                if (isWorldwide) remoteScope = "Worldwide";
                else if (isEurope) remoteScope = "Europe";
                else if (usOnly) remoteScope = "US-Only";

                bool isFranceCompatible = !usOnly && (isWorldwide || isEurope || !locLower.Contains("only"));

                // Contract format
                string contractType = rawJobType switch
                {
                    "contract" => "Freelance / Contractor",
                    "freelance" => "Freelance / Contractor",
                    "part_time" => "Temps partiel",
                    _ => "CDI / Full-time"
                };

                DateTime pubDate = DateTime.UtcNow;
                if (j.TryGetProperty("publication_date", out var pdProp) &&
                    DateTime.TryParse(pdProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var pd))
                {
                    pubDate = pd;
                }

                jobs.Add(new JobOffer
                {
                    ExternalId = $"remotive_{id}",
                    Source = "Remotive",
                    Title = title,
                    Company = company,
                    Location = location,
                    RemoteScope = remoteScope,
                    IsFranceCompatible = isFranceCompatible,
                    ContractType = contractType,
                    Salary = string.IsNullOrWhiteSpace(salary) ? null : salary,
                    ApplyUrl = jobUrl,
                    Description = cleanDesc,
                    Tags = string.Join(", ", tagsList),
                    PublishedAt = pubDate,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Discovered"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemotiveProvider] Error: {ex.Message}");
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
}

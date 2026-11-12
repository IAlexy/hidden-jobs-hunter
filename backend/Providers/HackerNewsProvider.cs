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

public class HackerNewsProvider : IJobProvider
{
    private const string AlgoliaApiHost = "hn.algolia.com";
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex UrlRegex = new(@"https?://[^\s""'<>]+", RegexOptions.Compiled, RegexTimeout);

    private readonly HttpClient _httpClient;

    public string Id => "HackerNews";
    public string Name => "Hacker News (Who is hiring?)";
    public string Description => "Offres directes postées chaque mois par les fondateurs et CTOs. Zéro intermédiaire, contacts directs.";
    public bool IsEnabledByDefault => true;

    public HackerNewsProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HiddenJobsHunter/1.0");
    }

    public async Task<List<JobOffer>> FetchJobsAsync(int limit = 50, CancellationToken ct = default)
    {
        var jobs = new List<JobOffer>();

        try
        {
            // 1. Get latest "Ask HN: Who is hiring?" thread
            var searchUrl = $"https://{AlgoliaApiHost}/api/v1/search_by_date?tags=story,author_whoishiring&query=Ask%20HN:%20Who%20is%20hiring&hitsPerPage=2";
            using var storyResponse = await _httpClient.GetAsync(searchUrl, ct);
            if (!storyResponse.IsSuccessStatusCode) return jobs;

            var storyDoc = await storyResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (!storyDoc.TryGetProperty("hits", out var hits) || hits.GetArrayLength() == 0) return jobs;

            var latestHiringStory = hits.EnumerateArray()
                .FirstOrDefault(h => h.GetProperty("title").GetString()?.Contains("Who is hiring", StringComparison.OrdinalIgnoreCase) == true);

            if (latestHiringStory.ValueKind == JsonValueKind.Undefined) return jobs;

            var storyId = latestHiringStory.GetProperty("objectID").GetString();
            if (string.IsNullOrEmpty(storyId)) return jobs;

            // 2. Fetch comments from this story
            var commentsUrl = $"https://{AlgoliaApiHost}/api/v1/search?tags=comment,story_{storyId}&hitsPerPage={Math.Max(limit * 2, 100)}";
            using var commentsResponse = await _httpClient.GetAsync(commentsUrl, ct);
            if (!commentsResponse.IsSuccessStatusCode) return jobs;

            var commentsDoc = await commentsResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (!commentsDoc.TryGetProperty("hits", out var commentHits)) return jobs;

            long numericStoryId = long.TryParse(storyId, out var parsedId) ? parsedId : 0;

            foreach (var comment in commentHits.EnumerateArray())
            {
                if (jobs.Count >= limit) break;

                // Check if top-level comment (direct job posting)
                if (comment.TryGetProperty("parent_id", out var parentIdProp) && parentIdProp.GetInt64() != numericStoryId)
                {
                    continue;
                }

                var commentId = comment.GetProperty("objectID").GetString() ?? Guid.NewGuid().ToString();
                var rawHtml = comment.TryGetProperty("comment_text", out var ctProp) ? ctProp.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(rawHtml)) continue;

                var text = CleanHtml(rawHtml);
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (lines.Length == 0) continue;

                var headerLine = lines[0];

                // Parse standard HN header: Company | Role | Location | Remote | Tech
                var parts = headerLine.Split('|', StringSplitOptions.TrimEntries);

                string company = parts.Length > 0 ? parts[0].Trim() : "HN Startup";
                string title = parts.Length > 1 ? parts[1].Trim() : "Software Engineer";
                string location = parts.Length > 2 ? parts[2].Trim() : "Remote";

                if (location.Length > 50)
                {
                    location = location.Contains("Remote", StringComparison.OrdinalIgnoreCase) ? "Remote" : location[..47] + "...";
                }
                if (company.Length > 50)
                {
                    company = company[..47] + "...";
                }

                // Filter out comments that are clearly NOT job postings (e.g. meta discussions)
                if (lines.Length < 3 && !text.Contains("hiring", StringComparison.OrdinalIgnoreCase)) continue;

                // Analyze remote eligibility
                bool mentionsRemote = text.Contains("REMOTE", StringComparison.OrdinalIgnoreCase);
                bool usOnly = text.Contains("US only", StringComparison.OrdinalIgnoreCase) ||
                              text.Contains("US citizen", StringComparison.OrdinalIgnoreCase) ||
                              text.Contains("North America only", StringComparison.OrdinalIgnoreCase);
                bool worldwide = text.Contains("worldwide", StringComparison.OrdinalIgnoreCase) ||
                                 text.Contains("anywhere", StringComparison.OrdinalIgnoreCase) ||
                                 text.Contains("global", StringComparison.OrdinalIgnoreCase);
                bool europe = text.Contains("Europe", StringComparison.OrdinalIgnoreCase) ||
                              text.Contains("EU", StringComparison.OrdinalIgnoreCase) ||
                              text.Contains("EMEA", StringComparison.OrdinalIgnoreCase) ||
                              text.Contains("France", StringComparison.OrdinalIgnoreCase);

                string remoteScope = "Remote";
                if (worldwide) remoteScope = "Worldwide";
                else if (europe) remoteScope = "Europe";
                else if (usOnly) remoteScope = "US-Only";
                else if (mentionsRemote) remoteScope = "Remote";

                bool isFranceCompatible = !usOnly && (worldwide || europe || mentionsRemote);

                // Extract direct contact
                string? directContact = null;
                var emailMatch = EmailRegex.Match(text);
                if (emailMatch.Success)
                {
                    directContact = emailMatch.Value;
                }

                // Extract apply URL
                string? applyUrl = $"https://news.ycombinator.com/item?id={commentId}";
                var urlMatches = UrlRegex.Matches(text);
                foreach (Match m in urlMatches)
                {
                    var u = m.Value.TrimEnd('.', ',', ')', '>');
                    if (u.Contains("greenhouse") || u.Contains("lever.co") || u.Contains("ashbyhq") ||
                        u.Contains("workable") || u.Contains("apply") || u.Contains("jobs") || u.Contains("careers"))
                    {
                        applyUrl = u;
                        break;
                    }
                }

                // Contract type
                string contractType = "CDI / Full-time";
                if (text.Contains("contract", StringComparison.OrdinalIgnoreCase) || text.Contains("freelance", StringComparison.OrdinalIgnoreCase))
                {
                    contractType = "Freelance / Contractor";
                }
                else if (text.Contains("part-time", StringComparison.OrdinalIgnoreCase))
                {
                    contractType = "Temps partiel";
                }

                // Tech tags
                var tags = ExtractTags(text);

                DateTime publishedAt = DateTime.UtcNow;
                if (comment.TryGetProperty("created_at", out var catProp) &&
                    DateTime.TryParse(catProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    publishedAt = dt;
                }

                jobs.Add(new JobOffer
                {
                    ExternalId = $"hn_{commentId}",
                    Source = "HackerNews",
                    Title = title.Length > 120 ? title[..117] + "..." : title,
                    Company = company.Length > 80 ? company[..77] + "..." : company,
                    Location = location,
                    RemoteScope = remoteScope,
                    IsFranceCompatible = isFranceCompatible,
                    ContractType = contractType,
                    DirectContact = directContact,
                    ApplyUrl = applyUrl,
                    Description = text,
                    Tags = string.Join(", ", tags),
                    PublishedAt = publishedAt,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Discovered"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HackerNewsProvider] Error: {ex.Message}");
        }

        return jobs;
    }

    private static string CleanHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return HtmlEntity.DeEntitize(doc.DocumentNode.InnerText).Trim();
    }

    private static List<string> ExtractTags(string content)
    {
        var keywords = new[]
        {
            ".NET", "C#", "React", "TypeScript", "JavaScript", "ASP.NET", "Next.js", "Node.js", "Node",
            "Python", "Go", "Golang", "Rust", "Vue", "Angular", "SQL", "PostgreSQL", "MongoDB", "Redis",
            "Docker", "Kubernetes", "Azure", "AWS", "GCP", "GraphQL", "REST", "Fullstack", "Backend", "Frontend",
            "Tailwind", "Kafka", "Microservices"
        };

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kw in keywords)
        {
            var pattern = kw is ".NET" or "C#"
                ? $@"(?<!\w){Regex.Escape(kw)}(?!\w)"
                : $@"\b{Regex.Escape(kw)}\b";
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase, RegexTimeout))
            {
                found.Add(kw);
            }
        }

        return found.ToList();
    }
}

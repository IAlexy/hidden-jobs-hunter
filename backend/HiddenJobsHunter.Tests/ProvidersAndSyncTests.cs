using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using HiddenJobsHunter.Api.Data;
using HiddenJobsHunter.Api.Models;
using HiddenJobsHunter.Api.Providers;
using HiddenJobsHunter.Api.Services;

namespace HiddenJobsHunter.Tests;

public class ProvidersAndSyncTests
{
    [Fact]
    public async Task RemotiveProvider_ParsesJobsAndEligibilityCorrectly()
    {
        const string json = @"{
            ""jobs"": [
                {
                    ""id"": 101,
                    ""title"": ""Senior .NET React Engineer"",
                    ""company_name"": ""RemotiveTech"",
                    ""url"": ""https://remotive.com/jobs/101"",
                    ""candidate_required_location"": ""Worldwide"",
                    ""job_type"": ""contract"",
                    ""salary"": ""$120k - $150k"",
                    ""description"": ""<p>We use C#, .NET 8 and React in full remote.</p>"",
                    ""tags"": ["".NET"", ""React"", ""TypeScript""],
                    ""publication_date"": ""2026-09-15T10:00:00""
                },
                {
                    ""id"": 102,
                    ""title"": ""US Backend Developer"",
                    ""company_name"": ""USCorp"",
                    ""url"": ""https://remotive.com/jobs/102"",
                    ""candidate_required_location"": ""USA Only"",
                    ""job_type"": ""full_time"",
                    ""description"": ""<p>US residents only.</p>"",
                    ""tags"": [""Go""]
                }
            ]
        }";

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));

        var provider = new RemotiveProvider(httpClient);
        var jobs = await provider.FetchJobsAsync(10);

        Assert.Equal(2, jobs.Count);
        Assert.Equal("remotive_101", jobs[0].ExternalId);
        Assert.Equal("Worldwide", jobs[0].RemoteScope);
        Assert.True(jobs[0].IsFranceCompatible);
        Assert.Equal("Freelance / Contractor", jobs[0].ContractType);
        Assert.Contains(".NET", jobs[0].Tags, StringComparison.Ordinal);

        Assert.Equal("remotive_102", jobs[1].ExternalId);
        Assert.Equal("US-Only", jobs[1].RemoteScope);
        Assert.False(jobs[1].IsFranceCompatible);
    }

    [Fact]
    public async Task JobicyProvider_ParsesJobsAndExtractsTechTags()
    {
        const string json = @"{
            ""jobs"": [
                {
                    ""id"": 202,
                    ""jobTitle"": ""Fullstack TypeScript & C# Developer"",
                    ""companyName"": ""JobicyStartup"",
                    ""url"": ""https://jobicy.com/jobs/202"",
                    ""jobGeo"": ""Europe"",
                    ""jobDescription"": ""<div>Build APIs with ASP.NET, Docker and PostgreSQL.</div>"",
                    ""annualSalaryMin"": ""80000"",
                    ""annualSalaryMax"": ""110000"",
                    ""salaryCurrency"": ""EUR"",
                    ""pubDate"": ""2026-09-16T08:30:00""
                }
            ]
        }";

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));

        var provider = new JobicyProvider(httpClient);
        var jobs = await provider.FetchJobsAsync(10);

        Assert.Single(jobs);
        Assert.Equal("jobicy_202", jobs[0].ExternalId);
        Assert.Equal("Europe", jobs[0].RemoteScope);
        Assert.True(jobs[0].IsFranceCompatible);
        Assert.Equal("80000 - 110000 EUR", jobs[0].Salary);
        Assert.Contains("C#", jobs[0].Tags, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TypeScript", jobs[0].Tags, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HackerNewsProvider_FetchesStoryAndParsesHiringComments()
    {
        const string storySearchJson = @"{
            ""hits"": [
                {
                    ""objectID"": ""400001"",
                    ""title"": ""Ask HN: Who is hiring? (September 2026)""
                }
            ]
        }";

        const string commentsJson = @"{
            ""hits"": [
                {
                    ""objectID"": ""400099"",
                    ""parent_id"": 400001,
                    ""created_at"": ""2026-09-01T14:00:00Z"",
                    ""comment_text"": ""CloudScale | Senior Fullstack Engineer | Remote (Worldwide) | Full-time / Contract<p>We are hiring a .NET, C#, React and TypeScript engineer.<p>Apply directly at founder@cloudscale.dev or https://jobs.ashbyhq.com/cloudscale""
                }
            ]
        }";

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            var payload = uri.Contains("search_by_date", StringComparison.OrdinalIgnoreCase)
                ? storySearchJson
                : commentsJson;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));

        var provider = new HackerNewsProvider(httpClient);
        var jobs = await provider.FetchJobsAsync(10);

        Assert.Single(jobs);
        Assert.Equal("hn_400099", jobs[0].ExternalId);
        Assert.Equal("CloudScale", jobs[0].Company);
        Assert.Equal("Senior Fullstack Engineer", jobs[0].Title);
        Assert.Equal("Worldwide", jobs[0].RemoteScope);
        Assert.True(jobs[0].IsFranceCompatible);
        Assert.Equal("founder@cloudscale.dev", jobs[0].DirectContact);
        Assert.Equal("https://jobs.ashbyhq.com/cloudscale", jobs[0].ApplyUrl);
        Assert.Contains("React", jobs[0].Tags, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CustomAtsProvider_SupportsGreenhouseLeverAshbyAndGenericHtml()
    {
        const string greenhouseJson = @"{
            ""jobs"": [
                {
                    ""id"": 501,
                    ""title"": ""Staff .NET Engineer"",
                    ""absolute_url"": ""https://boards.greenhouse.io/acme/jobs/501"",
                    ""location"": { ""name"": ""Remote - Worldwide"" },
                    ""content"": ""<p>Remote worldwide role using C#, .NET and React.</p>""
                }
            ]
        }";

        const string leverJson = @"[
            {
                ""id"": ""lev-601"",
                ""text"": ""Frontend React Engineer"",
                ""hostedUrl"": ""https://jobs.lever.co/buffer/lev-601"",
                ""descriptionPlain"": ""Remote TypeScript & React engineer."",
                ""categories"": { ""location"": ""Remote - Europe"" }
            }
        ]";

        const string ashbyJson = @"{
            ""jobs"": [
                {
                    ""id"": ""ash-701"",
                    ""title"": ""Fullstack TypeScript Engineer"",
                    ""jobUrl"": ""https://jobs.ashbyhq.com/linear/ash-701"",
                    ""location"": ""Remote""
                }
            ]
        }";

        const string genericHtml = @"
            <html>
                <head><title>NovaTech - Careers</title></head>
                <body>
                    <a href=""/careers/job-senior-dotnet"">Senior .NET & React Developer (Full Remote)</a>
                </body>
            </html>";

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri?.ToString() ?? string.Empty;
            if (url.Contains("greenhouse.io", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(greenhouseJson, Encoding.UTF8, "application/json")
                };
            }
            if (url.Contains("lever.co", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(leverJson, Encoding.UTF8, "application/json")
                };
            }
            if (url.Contains("ashbyhq.com", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ashbyJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(genericHtml, Encoding.UTF8, "text/html")
            };
        }));

        var provider = new CustomAtsProvider(httpClient);

        var ghJobs = await provider.IngestUrlAsync("https://boards.greenhouse.io/acme");
        Assert.Single(ghJobs);
        Assert.Equal("gh_acme_501", ghJobs[0].ExternalId);
        Assert.Equal("Worldwide", ghJobs[0].RemoteScope);

        var leverJobs = await provider.IngestUrlAsync("https://jobs.lever.co/buffer");
        Assert.Single(leverJobs);
        Assert.Equal("lever_buffer_lev-601", leverJobs[0].ExternalId);

        var ashbyJobs = await provider.IngestUrlAsync("https://jobs.ashbyhq.com/linear");
        Assert.Single(ashbyJobs);
        Assert.Equal("ashby_linear_ash-701", ashbyJobs[0].ExternalId);

        var htmlJobs = await provider.IngestUrlAsync("https://novatech.example.com/careers");
        Assert.Single(htmlJobs);
        Assert.Equal("NovaTech", htmlJobs[0].Company);
        Assert.Equal("https://novatech.example.com/careers/job-senior-dotnet", htmlJobs[0].ApplyUrl);
    }

    [Fact]
    public async Task JobSyncService_DeduplicatesSavesMatchesAndIngestsUrls()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(connection));

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""jobs"": [
                    {
                        ""id"": ""ash-999"",
                        ""title"": ""Senior C# React Engineer"",
                        ""jobUrl"": ""https://jobs.ashbyhq.com/demo/ash-999"",
                        ""location"": ""Remote Worldwide""
                    }
                ]
            }", Encoding.UTF8, "application/json")
        }));

        services.AddSingleton(httpClient);
        services.AddScoped<IAiMatcherService, AiMatcherService>();
        services.AddScoped<CustomAtsProvider>();
        services.AddScoped<IJobProvider>(_ => new StubJobProvider());
        services.AddScoped<JobSyncService>();

        using var sp = services.BuildServiceProvider();
        using (var initScope = sp.CreateScope())
        {
            var db = initScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.SeedDefaults();
            var settings = await db.Settings.FirstAsync();
            settings.OnlyFranceCompatibleJobs = true;
            await db.SaveChangesAsync();
        }

        var syncService = sp.GetRequiredService<JobSyncService>();

        // First sync: should add 1 France-compatible job and auto-match it
        var firstSync = await syncService.SyncAllProvidersAsync();
        Assert.Equal(2, firstSync.TotalFetched);
        Assert.Equal(1, firstSync.NewJobsSaved);
        Assert.Equal(1, firstSync.MatchesCreated);

        // Second sync: deduplication by ExternalId should add 0 new jobs
        var secondSync = await syncService.SyncAllProvidersAsync();
        Assert.Equal(0, secondSync.NewJobsSaved);

        // Ingest career URL
        var ingestRes = await syncService.IngestUrlAsync("https://jobs.ashbyhq.com/demo");
        Assert.Equal(1, ingestRes.ExtractedCount);
        Assert.Equal(1, ingestRes.NewJobsAdded);
    }

    private sealed class StubJobProvider : IJobProvider
    {
        public string Id => "Stub";
        public string Name => "Stub Provider";
        public string Description => "Test provider";
        public bool IsEnabledByDefault => true;

        public Task<List<JobOffer>> FetchJobsAsync(int limit = 50, System.Threading.CancellationToken ct = default)
        {
            return Task.FromResult(new List<JobOffer>
            {
                new()
                {
                    ExternalId = "stub_fr_1",
                    Source = "Stub",
                    Title = "Fullstack .NET React Developer",
                    Company = "RemoteEU",
                    RemoteScope = "Europe",
                    IsFranceCompatible = true,
                    Description = "C#, .NET 8, React, TypeScript in Europe."
                },
                new()
                {
                    ExternalId = "stub_us_2",
                    Source = "Stub",
                    Title = "US Only Engineer",
                    Company = "USOnlyInc",
                    RemoteScope = "US-Only",
                    IsFranceCompatible = false,
                    Description = "US citizens only."
                }
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HiddenJobsHunter.Api.Data;
using HiddenJobsHunter.Api.Models;
using HiddenJobsHunter.Api.Providers;

namespace HiddenJobsHunter.Tests;

public sealed class CustomWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hiddenjobs_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IJobProvider>();
            services.AddScoped<IJobProvider, FakeIntegrationJobProvider>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            TryDeleteFile(_dbPath);
            TryDeleteFile(_dbPath + "-shm");
            TryDeleteFile(_dbPath + "-wal");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // Ignore cleanup locks
        }
    }
}

internal sealed class FakeIntegrationJobProvider : IJobProvider
{
    public string Id => "HackerNews";
    public string Name => "Fake HN Provider";
    public string Description => "Fake provider for integration tests";
    public bool IsEnabledByDefault => true;

    public Task<List<JobOffer>> FetchJobsAsync(int limit = 50, CancellationToken ct = default)
    {
        return Task.FromResult(new List<JobOffer>
        {
            new()
            {
                ExternalId = "hn_int_1",
                Source = "HackerNews",
                Title = "Senior Fullstack .NET / React Engineer",
                Company = "CloudPulse",
                Location = "Remote (Europe)",
                RemoteScope = "Europe",
                IsFranceCompatible = true,
                ContractType = "CDI / Full-time",
                DirectContact = "cto@cloudpulse.io",
                ApplyUrl = "https://cloudpulse.io/jobs/1",
                Description = "We build distributed systems with .NET 8, C#, React, TypeScript and PostgreSQL.",
                Tags = ".NET, C#, React, TypeScript, PostgreSQL"
            },
            new()
            {
                ExternalId = "remotive_int_2",
                Source = "Remotive",
                Title = "Frontend React Developer",
                Company = "PixelWave",
                Location = "Worldwide",
                RemoteScope = "Worldwide",
                IsFranceCompatible = true,
                ContractType = "Freelance / Contractor",
                Description = "Looking for a React and TypeScript specialist for a B2B contract.",
                Tags = "React, TypeScript, Tailwind"
            }
        });
    }
}

public class ApiControllersIntegrationTests
{
    [Fact]
    public async Task SyncAndJobsControllers_FullWorkflow_WorksCorrectly()
    {
        using var factory = new CustomWebAppFactory();
        using var client = factory.CreateClient();

        // 1. Trigger sync
        var syncResponse = await client.PostAsync("/api/sync", null);
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);
        var syncJson = await syncResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, syncJson.GetProperty("newJobsSaved").GetInt32());
        Assert.Equal(2, syncJson.GetProperty("matchesCreated").GetInt32());

        // 2. Get jobs with filters
        var allJobsRes = await client.GetFromJsonAsync<JsonElement>("/api/jobs?franceCompatibleOnly=true&remoteOnly=true");
        Assert.Equal(2, allJobsRes.GetProperty("total").GetInt32());

        var hnJobsRes = await client.GetFromJsonAsync<JsonElement>("/api/jobs?source=HackerNews&search=CloudPulse&minScore=50");
        Assert.Equal(1, hnJobsRes.GetProperty("total").GetInt32());
        var firstJobId = hnJobsRes.GetProperty("items")[0].GetProperty("id").GetInt32();

        // 3. Get single job details
        var detailRes = await client.GetAsync($"/api/jobs/{firstJobId}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);

        // 4. Update job status and notes
        var statusRes = await client.PatchAsJsonAsync($"/api/jobs/{firstJobId}/status", new { status = "Saved", notes = "High priority CTO contact" });
        Assert.Equal(HttpStatusCode.OK, statusRes.StatusCode);

        var savedFilterRes = await client.GetFromJsonAsync<JsonElement>("/api/jobs?status=Saved");
        Assert.Equal(1, savedFilterRes.GetProperty("total").GetInt32());

        // 5. Stats endpoint
        var statsRes = await client.GetFromJsonAsync<JsonElement>("/api/jobs/stats");
        Assert.Equal(2, statsRes.GetProperty("totalJobs").GetInt32());
        Assert.Equal(1, statsRes.GetProperty("saved").GetInt32());

        // 6. Delete single job
        var delRes = await client.DeleteAsync($"/api/jobs/{firstJobId}");
        Assert.Equal(HttpStatusCode.OK, delRes.StatusCode);

        // 7. Clear remaining jobs
        var clearRes = await client.DeleteAsync("/api/jobs/clear");
        Assert.Equal(HttpStatusCode.OK, clearRes.StatusCode);
        var afterClear = await client.GetFromJsonAsync<JsonElement>("/api/jobs");
        Assert.Equal(0, afterClear.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task ProfilesController_CrudActivateParseAndPreventDeletingLastProfile()
    {
        using var factory = new CustomWebAppFactory();
        using var client = factory.CreateClient();

        // 1. Get seeded default profile (anonymized Demo Profile) and templates
        var templates = await client.GetFromJsonAsync<JsonElement>("/api/profiles/templates");
        Assert.True(templates.GetArrayLength() >= 6);
        Assert.Contains(templates.EnumerateArray(), t => t.GetProperty("id").GetString() == "data");
        Assert.Contains(templates.EnumerateArray(), t => t.GetProperty("id").GetString() == "design");

        var profiles = await client.GetFromJsonAsync<JsonElement>("/api/profiles");
        Assert.Equal(1, profiles.GetArrayLength());
        var defaultProfile = profiles[0];
        var defaultId = defaultProfile.GetProperty("id").GetInt32();
        var defaultName = defaultProfile.GetProperty("name").GetString() ?? string.Empty;
        Assert.Contains("Demo Profile", defaultName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Alexy", defaultName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("France", defaultProfile.GetProperty("country").GetString());
        Assert.Equal("software-dev", defaultProfile.GetProperty("jobCategory").GetString());

        // 2. Get active profile
        var activeRes = await client.GetFromJsonAsync<JsonElement>("/api/profiles/active");
        Assert.Equal(defaultId, activeRes.GetProperty("id").GetInt32());

        // 3. Refuse deleting the last remaining profile
        var deleteLastRes = await client.DeleteAsync($"/api/profiles/{defaultId}");
        Assert.Equal(HttpStatusCode.BadRequest, deleteLastRes.StatusCode);

        // 4. Create a second profile
        var createRes = await client.PostAsJsonAsync("/api/profiles", new
        {
            name = "Backend Go / Rust Profile",
            title = "Senior Backend Engineer",
            yearsOfExperience = 7,
            primarySkills = "Go, Rust, PostgreSQL, Docker",
            isActive = false
        });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var createdProfile = await createRes.Content.ReadFromJsonAsync<JsonElement>();
        var secondId = createdProfile.GetProperty("id").GetInt32();

        // 5. Update second profile
        var updateRes = await client.PutAsJsonAsync($"/api/profiles/{secondId}", new
        {
            name = "Updated Backend Profile",
            title = "Staff Backend Engineer",
            yearsOfExperience = 8,
            primarySkills = "Go, Rust, Kubernetes",
            secondarySkills = "AWS, Terraform",
            languages = "Français, Anglais",
            preferredContracts = "Freelance/Contractor",
            remoteScopePreference = "Worldwide",
            canWorkAsContractorB2B = true,
            preferredPitchLanguage = "English",
            resumeText = "8 years experience in Go and Rust."
        });
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        // 6. Activate second profile
        var activateRes = await client.PostAsync($"/api/profiles/{secondId}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activateRes.StatusCode);
        var newActive = await client.GetFromJsonAsync<JsonElement>("/api/profiles/active");
        Assert.Equal(secondId, newActive.GetProperty("id").GetInt32());

        // 7. Parse resume endpoint
        var parseRes = await client.PostAsJsonAsync("/api/profiles/parse-resume", new
        {
            cvText = "Senior Fullstack Engineer with 6 years of experience in .NET, C#, React, TypeScript and Docker. Languages: Français, English.",
            profileName = "Parsed Profile"
        });
        Assert.Equal(HttpStatusCode.OK, parseRes.StatusCode);
        var parsedJson = await parseRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(6, parsedJson.GetProperty("yearsOfExperience").GetInt32());

        // 8. Delete second profile (now allowed because 2 profiles exist)
        var deleteSecondRes = await client.DeleteAsync($"/api/profiles/{secondId}");
        Assert.Equal(HttpStatusCode.OK, deleteSecondRes.StatusCode);
    }

    [Fact]
    public async Task MatchingController_SingleMatchMatchAllAndPitchGeneration_WorkEndToEnd()
    {
        using var factory = new CustomWebAppFactory();
        using var client = factory.CreateClient();

        await client.PostAsync("/api/sync", null);
        var jobsRes = await client.GetFromJsonAsync<JsonElement>("/api/jobs");
        var jobId = jobsRes.GetProperty("items")[0].GetProperty("id").GetInt32();

        // 1. Re-match single job
        var singleMatchRes = await client.PostAsync($"/api/matching/job/{jobId}", null);
        Assert.Equal(HttpStatusCode.OK, singleMatchRes.StatusCode);
        var matchJson = await singleMatchRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(matchJson.GetProperty("matchScore").GetInt32() > 0);

        // 2. Match all jobs
        var matchAllRes = await client.PostAsync("/api/matching/match-all", null);
        Assert.Equal(HttpStatusCode.OK, matchAllRes.StatusCode);

        // 3. Generate pitch via /api/matching/job/{id}/pitch
        var pitchRouteRes = await client.PostAsJsonAsync($"/api/matching/job/{jobId}/pitch", new
        {
            customInstructions = "Highlight B2B availability"
        });
        Assert.Equal(HttpStatusCode.OK, pitchRouteRes.StatusCode);
        var pitchJson = await pitchRouteRes.Content.ReadFromJsonAsync<JsonElement>();
        var pitchText = pitchJson.GetProperty("pitch").GetString() ?? string.Empty;
        Assert.Contains("Highlight B2B availability", pitchText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SettingsController_MasksApiKeysAndPreservesMaskedValuesOnUpdate()
    {
        using var factory = new CustomWebAppFactory();
        using var client = factory.CreateClient();

        // 1. Save real API keys
        var putRes = await client.PutAsJsonAsync("/api/settings", new
        {
            aiProvider = "Gemini",
            geminiApiKey = "AIzaSySecretKey123456789",
            geminiModel = "gemini-2.5-flash",
            openAiApiKey = "sk-proj-OpenAiSecret987654",
            openAiModel = "gpt-4o-mini",
            ollamaEndpoint = "http://localhost:11434",
            ollamaModel = "llama3",
            autoMatchOnSync = true,
            onlyFranceCompatibleJobs = true
        });
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);

        // 2. GET /api/settings must return masked keys containing ********
        var getJson = await client.GetFromJsonAsync<JsonElement>("/api/settings");
        var maskedGemini = getJson.GetProperty("geminiApiKey").GetString() ?? string.Empty;
        var maskedOpenAi = getJson.GetProperty("openAiApiKey").GetString() ?? string.Empty;
        Assert.Contains("********", maskedGemini, StringComparison.Ordinal);
        Assert.DoesNotContain("SecretKey", maskedGemini, StringComparison.Ordinal);
        Assert.Contains("********", maskedOpenAi, StringComparison.Ordinal);

        // 3. PUT /api/settings with masked key preserves the underlying secret key in DB
        var putMaskedRes = await client.PutAsJsonAsync("/api/settings", new
        {
            aiProvider = "Heuristic",
            geminiApiKey = maskedGemini,
            openAiApiKey = maskedOpenAi,
            autoMatchOnSync = false,
            onlyFranceCompatibleJobs = true
        });
        Assert.Equal(HttpStatusCode.OK, putMaskedRes.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbSettings = await db.Settings.FirstAsync();
        Assert.Equal("AIzaSySecretKey123456789", dbSettings.GeminiApiKey);
        Assert.Equal("sk-proj-OpenAiSecret987654", dbSettings.OpenAiApiKey);
        Assert.False(dbSettings.AutoMatchOnSync);
    }

    [Fact]
    public async Task SyncController_IngestUrl_ValidatesInvalidUrls()
    {
        using var factory = new CustomWebAppFactory();
        using var client = factory.CreateClient();

        var emptyRes = await client.PostAsJsonAsync("/api/sync/ingest-url", new { url = "" });
        Assert.Equal(HttpStatusCode.BadRequest, emptyRes.StatusCode);

        var invalidRes = await client.PostAsJsonAsync("/api/sync/ingest-url", new { url = "not-a-valid-url" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidRes.StatusCode);
    }
}

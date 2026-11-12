using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HiddenJobsHunter.Api.Data;
using HiddenJobsHunter.Api.Models;
using HiddenJobsHunter.Api.Providers;

namespace HiddenJobsHunter.Api.Services;

public class JobSyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSyncService> _logger;

    public JobSyncService(IServiceProvider serviceProvider, ILogger<JobSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IReadOnlyList<ProviderInfoDto> GetProviders()
    {
        using var scope = _serviceProvider.CreateScope();
        var providers = scope.ServiceProvider.GetServices<IJobProvider>();
        return providers.Select(p => new ProviderInfoDto(
            p.Id,
            p.Name,
            p.Description,
            p.IsEnabledByDefault)).ToList();
    }

    public async Task<SyncResult> SyncAllProvidersAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiMatcherService>();
        var providers = scope.ServiceProvider.GetServices<IJobProvider>().ToList();

        var settings = await db.Settings.FirstOrDefaultAsync(ct) ?? new AppSettings();
        var activeProfile = await db.UserProfiles.FirstOrDefaultAsync(p => p.IsActive, ct);

        int totalFetched = 0;
        int newJobsSaved = 0;
        int matchesCreated = 0;
        var details = new List<string>();

        foreach (var provider in providers)
        {
            try
            {
                _logger.LogInformation("Syncing provider {ProviderName}...", provider.Name);
                var jobs = await provider.FetchJobsAsync(50, ct);
                totalFetched += jobs.Count;

                int providerAdded = 0;
                foreach (var job in jobs)
                {
                    if (settings.OnlyFranceCompatibleJobs && !job.IsFranceCompatible)
                    {
                        continue;
                    }

                    bool exists = await db.JobOffers.AnyAsync(j => j.ExternalId == job.ExternalId, ct);
                    if (!exists)
                    {
                        db.JobOffers.Add(job);
                        await db.SaveChangesAsync(ct);
                        newJobsSaved++;
                        providerAdded++;

                        if (settings.AutoMatchOnSync && activeProfile != null)
                        {
                            try
                            {
                                var match = await aiService.MatchJobAsync(job, activeProfile, settings, ct);
                                db.JobMatches.Add(match);
                                await db.SaveChangesAsync(ct);
                                matchesCreated++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Auto-match failed for job {JobId}: {Message}", job.Id, ex.Message);
                            }
                        }
                    }
                }

                details.Add($"{provider.Name}: {jobs.Count} récupérées, {providerAdded} nouvelles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing provider {ProviderId}", provider.Id);
                details.Add($"{provider.Name}: Erreur ({ex.Message})");
            }
        }

        return new SyncResult
        {
            TotalFetched = totalFetched,
            NewJobsSaved = newJobsSaved,
            MatchesCreated = matchesCreated,
            Details = details,
            SyncedAt = DateTime.UtcNow
        };
    }

    public async Task<IngestUrlResult> IngestUrlAsync(string url, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiMatcherService>();
        var customAtsProvider = scope.ServiceProvider.GetRequiredService<CustomAtsProvider>();

        var jobs = await customAtsProvider.IngestUrlAsync(url, ct);
        var activeProfile = await db.UserProfiles.FirstOrDefaultAsync(p => p.IsActive, ct);
        var settings = await db.Settings.FirstOrDefaultAsync(ct) ?? new AppSettings();

        int added = 0;
        foreach (var job in jobs)
        {
            bool exists = await db.JobOffers.AnyAsync(j => j.ExternalId == job.ExternalId, ct);
            if (!exists)
            {
                db.JobOffers.Add(job);
                await db.SaveChangesAsync(ct);
                added++;

                if (settings.AutoMatchOnSync && activeProfile != null)
                {
                    try
                    {
                        var match = await aiService.MatchJobAsync(job, activeProfile, settings, ct);
                        db.JobMatches.Add(match);
                        await db.SaveChangesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Auto-match failed for ingested job {JobId}", job.Id);
                    }
                }
            }
        }

        return new IngestUrlResult(url, jobs.Count, added);
    }
}

public record ProviderInfoDto(string Id, string Name, string Description, bool IsEnabledByDefault);

public record IngestUrlResult(string Url, int ExtractedCount, int NewJobsAdded);

public class SyncResult
{
    public int TotalFetched { get; set; }
    public int NewJobsSaved { get; set; }
    public int MatchesCreated { get; set; }
    public List<string> Details { get; set; } = new();
    public DateTime SyncedAt { get; set; }
}

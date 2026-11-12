using System.Threading;
using System.Threading.Tasks;
using HiddenJobsHunter.Api.Models;

namespace HiddenJobsHunter.Api.Services;

public interface IAiMatcherService
{
    Task<JobMatch> MatchJobAsync(JobOffer job, UserProfile profile, AppSettings settings, CancellationToken ct = default);
    Task<JobMatch> EvaluateMatchAsync(JobOffer job, UserProfile profile, AppSettings settings, CancellationToken ct = default);
    Task<string> GeneratePitchAsync(JobOffer job, UserProfile profile, AppSettings settings, string? customInstructions = null, CancellationToken ct = default);
    Task<UserProfile> ParseCvToProfileAsync(string cvText, string profileName, AppSettings settings, CancellationToken ct = default);
    Task<UserProfile> ParseResumeAsync(string cvText, string profileName, AppSettings settings, CancellationToken ct = default);
}

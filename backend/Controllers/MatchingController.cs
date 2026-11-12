using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HiddenJobsHunter.Api.Data;
using HiddenJobsHunter.Api.Models;
using HiddenJobsHunter.Api.Services;

namespace HiddenJobsHunter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiMatcherService _aiService;
    private readonly ILogger<MatchingController> _logger;

    public MatchingController(AppDbContext db, IAiMatcherService aiService, ILogger<MatchingController> logger)
    {
        _db = db;
        _aiService = aiService;
        _logger = logger;
    }

    [HttpPost("job/{jobId:int}")]
    public async Task<IActionResult> MatchSingleJob(int jobId, [FromQuery] int? profileId = null)
    {
        var job = await _db.JobOffers.FindAsync(jobId);
        if (job == null) return NotFound("Offre introuvable.");

        var profile = profileId.HasValue
            ? await _db.UserProfiles.FindAsync(profileId.Value)
            : await _db.UserProfiles.FirstOrDefaultAsync(p => p.IsActive);

        if (profile == null) return BadRequest("Aucun profil actif trouvé.");

        var settings = await _db.Settings.FirstOrDefaultAsync() ?? new AppSettings();

        var existing = await _db.JobMatches
            .FirstOrDefaultAsync(m => m.JobOfferId == jobId && m.UserProfileId == profile.Id);

        var match = await _aiService.MatchJobAsync(job, profile, settings);

        if (existing != null)
        {
            existing.MatchScore = match.MatchScore;
            existing.TechStackFit = match.TechStackFit;
            existing.RemoteEligibilityFit = match.RemoteEligibilityFit;
            existing.Summary = match.Summary;
            existing.StrengthsJson = match.StrengthsJson;
            existing.MissingSkillsJson = match.MissingSkillsJson;
            existing.AdministrativeFeasibility = match.AdministrativeFeasibility;
            existing.TailoredPitch = match.TailoredPitch;
            existing.AiProviderUsed = match.AiProviderUsed;
            existing.AnalyzedAt = DateTime.UtcNow;
        }
        else
        {
            _db.JobMatches.Add(match);
        }

        await _db.SaveChangesAsync();
        return Ok(existing ?? match);
    }

    [HttpPost("batch")]
    [HttpPost("match-all")]
    public async Task<IActionResult> BatchMatch([FromQuery] int? profileId = null, [FromQuery] int limit = 50)
    {
        var profile = profileId.HasValue
            ? await _db.UserProfiles.FindAsync(profileId.Value)
            : await _db.UserProfiles.FirstOrDefaultAsync(p => p.IsActive);

        if (profile == null) return BadRequest("Aucun profil actif trouvé.");

        var settings = await _db.Settings.FirstOrDefaultAsync() ?? new AppSettings();

        var jobs = await _db.JobOffers
            .Where(j => !j.Matches.Any(m => m.UserProfileId == profile.Id))
            .Take(limit)
            .ToListAsync();

        int matchedCount = 0;
        foreach (var job in jobs)
        {
            try
            {
                var match = await _aiService.MatchJobAsync(job, profile, settings);
                _db.JobMatches.Add(match);
                matchedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Batch match failed for job {JobId}", job.Id);
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { Processed = jobs.Count, Matched = matchedCount });
    }

    [HttpPost("pitch")]
    public async Task<IActionResult> GeneratePitch([FromBody] GeneratePitchRequest req)
    {
        if (!req.JobId.HasValue) return BadRequest("JobId est requis.");
        return await GeneratePitchInternalAsync(req.JobId.Value, req.ProfileId, req.CustomInstructions);
    }

    [HttpPost("job/{jobId:int}/pitch")]
    public async Task<IActionResult> GeneratePitchForJob(int jobId, [FromBody] GeneratePitchByRouteRequest? req = null)
    {
        return await GeneratePitchInternalAsync(jobId, req?.ProfileId, req?.CustomInstructions);
    }

    private async Task<IActionResult> GeneratePitchInternalAsync(int jobId, int? profileId, string? customInstructions)
    {
        var job = await _db.JobOffers.FindAsync(jobId);
        if (job == null) return NotFound("Offre introuvable.");

        var profile = profileId.HasValue
            ? await _db.UserProfiles.FindAsync(profileId.Value)
            : await _db.UserProfiles.FirstOrDefaultAsync(p => p.IsActive);

        if (profile == null) return BadRequest("Profil introuvable.");

        var settings = await _db.Settings.FirstOrDefaultAsync() ?? new AppSettings();
        var pitch = await _aiService.GeneratePitchAsync(job, profile, settings, customInstructions);

        var match = await _db.JobMatches.FirstOrDefaultAsync(m => m.JobOfferId == jobId && m.UserProfileId == profile.Id);
        if (match != null)
        {
            match.TailoredPitch = pitch;
            await _db.SaveChangesAsync();
        }

        return Ok(new { Pitch = pitch });
    }
}

public record GeneratePitchRequest(int? JobId = null, int? ProfileId = null, string? CustomInstructions = null);
public record GeneratePitchByRouteRequest(int? ProfileId = null, string? CustomInstructions = null);

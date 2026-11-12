using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HiddenJobsHunter.Api.Data;

namespace HiddenJobsHunter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _db;

    public JobsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? search = null,
        [FromQuery] string? source = null,
        [FromQuery] string? remoteScope = null,
        [FromQuery] string? contractType = null,
        [FromQuery] string? status = null,
        [FromQuery] int? minScore = null,
        [FromQuery] bool? remoteOnly = null,
        [FromQuery] bool? franceCompatibleOnly = null,
        [FromQuery] int? profileId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var activeProfile = profileId.HasValue
            ? await _db.UserProfiles.FindAsync(profileId.Value)
            : await _db.UserProfiles.FirstOrDefaultAsync(p => p.IsActive);

        int currentProfileId = activeProfile?.Id ?? 0;

        var query = _db.JobOffers
            .Include(j => j.Matches.Where(m => m.UserProfileId == currentProfileId))
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(j =>
                j.Title.ToLower().Contains(s) ||
                j.Company.ToLower().Contains(s) ||
                j.Tags.ToLower().Contains(s) ||
                j.Description.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(source) && source != "All")
        {
            query = query.Where(j => j.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(remoteScope) && remoteScope != "All")
        {
            query = query.Where(j => j.RemoteScope == remoteScope);
        }

        if (remoteOnly == true)
        {
            query = query.Where(j => j.RemoteScope != "On-Site" && j.Location != "On-Site");
        }

        if (!string.IsNullOrWhiteSpace(contractType) && contractType != "All")
        {
            query = query.Where(j => j.ContractType.Contains(contractType));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(j => j.Status == status);
        }

        if (franceCompatibleOnly == true)
        {
            query = query.Where(j => j.IsFranceCompatible);
        }

        if (minScore.HasValue && minScore.Value > 0)
        {
            query = query.Where(j => j.Matches.Any(m => m.UserProfileId == currentProfileId && m.MatchScore >= minScore.Value));
        }

        var total = await query.CountAsync();

        var jobs = await query
            .OrderByDescending(j => j.Matches.Where(m => m.UserProfileId == currentProfileId).Select(m => (int?)m.MatchScore).FirstOrDefault() ?? 0)
            .ThenByDescending(j => j.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id,
                j.ExternalId,
                j.Source,
                j.Title,
                j.Company,
                j.CompanyUrl,
                j.Location,
                j.RemoteScope,
                j.IsFranceCompatible,
                j.ContractType,
                j.Salary,
                j.DirectContact,
                j.ApplyUrl,
                DescriptionPreview = j.Description.Length > 280 ? j.Description.Substring(0, 277) + "..." : j.Description,
                j.Tags,
                j.PublishedAt,
                j.CreatedAt,
                j.Status,
                j.UserNotes,
                ActiveMatch = j.Matches.FirstOrDefault(m => m.UserProfileId == currentProfileId)
            })
            .ToListAsync();

        return Ok(new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            ActiveProfile = activeProfile == null ? null : new { activeProfile.Id, activeProfile.Name, activeProfile.Title },
            Items = jobs
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetJob(int id, [FromQuery] int? profileId = null)
    {
        var activeProfile = profileId.HasValue
            ? await _db.UserProfiles.FindAsync(profileId.Value)
            : await _db.UserProfiles.FirstOrDefaultAsync(p => p.IsActive);

        int currentProfileId = activeProfile?.Id ?? 0;

        var job = await _db.JobOffers
            .Include(j => j.Matches.Where(m => m.UserProfileId == currentProfileId))
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();

        return Ok(new
        {
            job.Id,
            job.ExternalId,
            job.Source,
            job.Title,
            job.Company,
            job.CompanyUrl,
            job.Location,
            job.RemoteScope,
            job.IsFranceCompatible,
            job.ContractType,
            job.Salary,
            job.DirectContact,
            job.ApplyUrl,
            job.Description,
            job.Tags,
            job.PublishedAt,
            job.CreatedAt,
            job.Status,
            job.UserNotes,
            ActiveMatch = job.Matches.FirstOrDefault(m => m.UserProfileId == currentProfileId)
        });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var job = await _db.JobOffers.FindAsync(id);
        if (job == null) return NotFound();

        job.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.Notes))
        {
            job.UserNotes = dto.Notes;
        }

        await _db.SaveChangesAsync();
        return Ok(new { job.Id, job.Status, job.UserNotes });
    }

    [HttpPatch("{id:int}/notes")]
    public async Task<IActionResult> UpdateNotes(int id, [FromBody] UpdateNotesDto dto)
    {
        var job = await _db.JobOffers.FindAsync(id);
        if (job == null) return NotFound();

        job.UserNotes = dto.Notes;
        await _db.SaveChangesAsync();
        return Ok(new { job.Id, job.UserNotes });
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearJobs()
    {
        var allJobs = await _db.JobOffers.ToListAsync();
        int count = allJobs.Count;
        _db.JobOffers.RemoveRange(allJobs);
        await _db.SaveChangesAsync();
        return Ok(new { Deleted = count });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var job = await _db.JobOffers.FindAsync(id);
        if (job == null) return NotFound();

        _db.JobOffers.Remove(job);
        await _db.SaveChangesAsync();
        return Ok(new { DeletedId = id });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? profileId = null)
    {
        var activeProfile = profileId.HasValue
            ? await _db.UserProfiles.FindAsync(profileId.Value)
            : await _db.UserProfiles.FirstOrDefaultAsync(p => p.IsActive);

        int currentProfileId = activeProfile?.Id ?? 0;

        var totalJobs = await _db.JobOffers.CountAsync();
        var franceCompatible = await _db.JobOffers.CountAsync(j => j.IsFranceCompatible);
        var hnJobs = await _db.JobOffers.CountAsync(j => j.Source == "HackerNews");
        var savedJobs = await _db.JobOffers.CountAsync(j => j.Status == "Saved");
        var appliedJobs = await _db.JobOffers.CountAsync(j => j.Status == "Applied");
        var interviewJobs = await _db.JobOffers.CountAsync(j => j.Status == "Interview");
        var directContactJobs = await _db.JobOffers.CountAsync(j => j.DirectContact != null);

        var highMatches = await _db.JobMatches
            .CountAsync(m => m.UserProfileId == currentProfileId && m.MatchScore >= 75);

        return Ok(new
        {
            TotalJobs = totalJobs,
            FranceCompatible = franceCompatible,
            HnDirectPosts = hnJobs,
            WithDirectContact = directContactJobs,
            HighMatches = highMatches,
            Saved = savedJobs,
            Applied = appliedJobs,
            Interview = interviewJobs
        });
    }
}

public record UpdateStatusDto(string Status, string? Notes = null);
public record UpdateNotesDto(string? Notes);

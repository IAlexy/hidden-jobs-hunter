using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HiddenJobsHunter.Api.Data;
using HiddenJobsHunter.Api.Models;
using HiddenJobsHunter.Api.Services;

namespace HiddenJobsHunter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiMatcherService _aiService;

    public ProfilesController(AppDbContext db, IAiMatcherService aiService)
    {
        _db = db;
        _aiService = aiService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfiles()
    {
        var profiles = await _db.UserProfiles
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.IsActive,
                p.Title,
                p.YearsOfExperience,
                p.PreferredContracts,
                p.RemoteScopePreference,
                p.CanWorkAsContractorB2B,
                p.PrimarySkills,
                p.SecondarySkills,
                p.Languages,
                p.ExpectedSalaryOrTjm,
                p.PreferredPitchLanguage,
                p.ResumeText,
                p.UpdatedAt,
                MatchCount = p.Matches.Count,
                HighMatchCount = p.Matches.Count(m => m.MatchScore >= 75)
            })
            .ToListAsync();

        return Ok(profiles);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveProfile()
    {
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.IsActive)
                      ?? await _db.UserProfiles.FirstOrDefaultAsync();
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var profile = await _db.UserProfiles.FindAsync(id);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProfile([FromBody] UserProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            return BadRequest("Le nom du profil est obligatoire.");
        }

        bool isFirst = !await _db.UserProfiles.AnyAsync();
        if (profile.IsActive || isFirst)
        {
            var others = await _db.UserProfiles.Where(p => p.IsActive).ToListAsync();
            foreach (var o in others) o.IsActive = false;
            profile.IsActive = true;
        }

        profile.CreatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        _db.UserProfiles.Add(profile);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, profile);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UserProfile updated)
    {
        var profile = await _db.UserProfiles.FindAsync(id);
        if (profile == null) return NotFound();

        profile.Name = updated.Name;
        profile.Title = updated.Title;
        profile.YearsOfExperience = updated.YearsOfExperience;
        profile.PreferredContracts = updated.PreferredContracts;
        profile.RemoteScopePreference = updated.RemoteScopePreference;
        profile.CanWorkAsContractorB2B = updated.CanWorkAsContractorB2B;
        profile.PrimarySkills = updated.PrimarySkills;
        profile.SecondarySkills = updated.SecondarySkills;
        profile.Languages = updated.Languages;
        profile.ExpectedSalaryOrTjm = updated.ExpectedSalaryOrTjm;
        profile.PreferredPitchLanguage = updated.PreferredPitchLanguage;
        profile.ResumeText = updated.ResumeText;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(profile);
    }

    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> ActivateProfile(int id)
    {
        var profile = await _db.UserProfiles.FindAsync(id);
        if (profile == null) return NotFound();

        var activeOnes = await _db.UserProfiles.Where(p => p.IsActive && p.Id != id).ToListAsync();
        foreach (var a in activeOnes) a.IsActive = false;

        profile.IsActive = true;
        await _db.SaveChangesAsync();

        return Ok(new { profile.Id, profile.Name, profile.IsActive });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProfile(int id)
    {
        var profile = await _db.UserProfiles.FindAsync(id);
        if (profile == null) return NotFound();

        int count = await _db.UserProfiles.CountAsync();
        if (count <= 1)
        {
            return BadRequest("Impossible de supprimer le dernier profil.");
        }

        _db.UserProfiles.Remove(profile);
        await _db.SaveChangesAsync();

        var remaining = await _db.UserProfiles.FirstOrDefaultAsync();
        if (remaining != null && !await _db.UserProfiles.AnyAsync(p => p.IsActive))
        {
            remaining.IsActive = true;
            await _db.SaveChangesAsync();
        }

        return Ok(new { DeletedId = id });
    }

    [HttpPost("parse-cv")]
    [HttpPost("parse-resume")]
    public async Task<IActionResult> ParseCv([FromBody] ParseCvRequest req)
    {
        var text = !string.IsNullOrWhiteSpace(req.CvText) ? req.CvText : req.ResumeText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest("Le texte du CV ne peut pas être vide.");
        }

        var settings = await _db.Settings.FirstOrDefaultAsync() ?? new AppSettings();
        var profile = await _aiService.ParseCvToProfileAsync(text, req.ProfileName ?? "Nouveau profil", settings);

        return Ok(profile);
    }
}

public record ParseCvRequest(string? CvText = null, string? ResumeText = null, string? ProfileName = null);

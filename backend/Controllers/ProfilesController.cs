using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    private static readonly IReadOnlyList<ProfileTemplateDto> BuiltInTemplates = new List<ProfileTemplateDto>
    {
        new(
            Id: "software-dev",
            Name: "Fullstack Software Engineer",
            Title: "Senior Fullstack Software Engineer",
            JobCategory: "software-dev",
            YearsOfExperience: 5,
            PrimarySkills: ".NET, C#, React, TypeScript, Node.js, PostgreSQL, REST APIs, GraphQL",
            SecondarySkills: "Docker, AWS, Azure, Next.js, TailwindCSS, Redis, CI/CD",
            PreferredContracts: "CDI,Freelance/Contractor",
            ExpectedSalaryOrTjm: "CDI: 65k-85k€ / Freelance: 500-650€ TJM / International: $90k-$140k"
        ),
        new(
            Id: "data",
            Name: "Data Scientist / AI Engineer",
            Title: "Senior Data Scientist & AI/ML Engineer",
            JobCategory: "data",
            YearsOfExperience: 5,
            PrimarySkills: "Python, PyTorch, TensorFlow, LLM, RAG, Pandas, SQL, Spark, Machine Learning",
            SecondarySkills: "Snowflake, dbt, Airflow, Docker, AWS, MLOps, Hugging Face",
            PreferredContracts: "CDI,Freelance/Contractor",
            ExpectedSalaryOrTjm: "CDI: 70k-95k€ / Freelance: 550-750€ TJM / International: $100k-$160k"
        ),
        new(
            Id: "devops",
            Name: "DevOps / Cloud Architect",
            Title: "Senior DevOps & Cloud Platform Architect",
            JobCategory: "devops",
            YearsOfExperience: 6,
            PrimarySkills: "Kubernetes, Terraform, AWS, GCP, Azure, Docker, CI/CD, Linux, Go",
            SecondarySkills: "Prometheus, Grafana, Ansible, Helm, ArgoCD, Datadog, GitHub Actions",
            PreferredContracts: "CDI,Freelance/Contractor",
            ExpectedSalaryOrTjm: "CDI: 70k-90k€ / Freelance: 550-700€ TJM / International: $100k-$150k"
        ),
        new(
            Id: "product",
            Name: "Product Manager (Tech / SaaS)",
            Title: "Senior Product Manager",
            JobCategory: "product",
            YearsOfExperience: 5,
            PrimarySkills: "Product Discovery, Roadmap, Scrum, Agile, User Research, Product Strategy, SQL, A/B Testing",
            SecondarySkills: "Jira, Notion, Linear, Figma, Amplitude, Mixpanel, Stakeholder Management",
            PreferredContracts: "CDI,Freelance/Contractor",
            ExpectedSalaryOrTjm: "CDI: 65k-85k€ / Freelance: 500-650€ TJM / International: $90k-$140k"
        ),
        new(
            Id: "design",
            Name: "UX/UI Product Designer",
            Title: "Senior UX/UI Product Designer",
            JobCategory: "design",
            YearsOfExperience: 5,
            PrimarySkills: "Figma, Design Systems, UX Research, Prototyping, Wireframing, UI Design, Usability Testing",
            SecondarySkills: "Framer, Webflow, HTML, CSS, Accessibility, Workshop Facilitation",
            PreferredContracts: "CDI,Freelance/Contractor",
            ExpectedSalaryOrTjm: "CDI: 55k-75k€ / Freelance: 450-600€ TJM / International: $80k-$125k"
        ),
        new(
            Id: "marketing",
            Name: "Growth & Digital Marketing",
            Title: "Head of Growth & Product Marketing",
            JobCategory: "marketing",
            YearsOfExperience: 5,
            PrimarySkills: "Growth Hacking, SEO, SEM, HubSpot, Google Analytics, Copywriting, CRM, Demand Generation",
            SecondarySkills: "Product Marketing, Content Strategy, Paid Social, Email Automation, A/B Testing",
            PreferredContracts: "CDI,Freelance/Contractor",
            ExpectedSalaryOrTjm: "CDI: 55k-80k€ / Freelance: 450-600€ TJM / International: $80k-$130k"
        ),
        new(
            Id: "cybersecurity",
            Name: "Cybersecurity / SecOps Engineer",
            Title: "Senior Cybersecurity & SecOps Engineer",
            JobCategory: "cybersecurity",
            YearsOfExperience: 6,
            PrimarySkills: "Pentest, SOC, SIEM, Zero Trust, Cloud Security, AppSec, IAM, Incident Response",
            SecondarySkills: "ISO 27001, SOC2, AWS Security, Kubernetes, Python, Threat Hunting",
            PreferredContracts: "CDI,Freelance/Contractor",
            ExpectedSalaryOrTjm: "CDI: 70k-95k€ / Freelance: 600-800€ TJM / International: $100k-$155k"
        )
    };

    private readonly AppDbContext _db;
    private readonly IAiMatcherService _aiService;

    public ProfilesController(AppDbContext db, IAiMatcherService aiService)
    {
        _db = db;
        _aiService = aiService;
    }

    [HttpGet("templates")]
    [ProducesResponseType(typeof(IEnumerable<ProfileTemplateDto>), StatusCodes.Status200OK)]
    public IActionResult GetProfileTemplates()
    {
        return Ok(BuiltInTemplates);
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
                p.Country,
                p.JobCategory,
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
        profile.Country = string.IsNullOrWhiteSpace(updated.Country) ? profile.Country : updated.Country;
        profile.JobCategory = string.IsNullOrWhiteSpace(updated.JobCategory) ? profile.JobCategory : updated.JobCategory;
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

public record ProfileTemplateDto(
    string Id,
    string Name,
    string Title,
    string JobCategory,
    int YearsOfExperience,
    string PrimarySkills,
    string SecondarySkills,
    string PreferredContracts,
    string ExpectedSalaryOrTjm
);

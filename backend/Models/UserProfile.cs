using System;
using System.Collections.Generic;

namespace HiddenJobsHunter.Api.Models;

public class UserProfile
{
    public int Id { get; set; } = 0;
    
    public required string Name { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public string Title { get; set; } = "Fullstack Software Engineer";
    
    public int YearsOfExperience { get; set; } = 5;
    
    // Comma-separated: "CDI,Freelance/Contractor,Temps partiel"
    public string PreferredContracts { get; set; } = "CDI,Freelance/Contractor";
    
    // Comma-separated: "Worldwide,Europe,France"
    public string RemoteScopePreference { get; set; } = "Worldwide,Europe,France";
    
    // If true, profile can work as French micro-entrepreneur / SASU contractor for overseas firms
    public bool CanWorkAsContractorB2B { get; set; } = true;
    
    public string PrimarySkills { get; set; } = ".NET, C#, React, TypeScript, ASP.NET Core, SQL, REST APIs";
    
    public string SecondarySkills { get; set; } = "Docker, Azure, TailwindCSS, Next.js, Redis, CI/CD";
    
    public string Languages { get; set; } = "Français (natif), Anglais (courant / bilingue)";
    
    public string? ExpectedSalaryOrTjm { get; set; } = "CDI: 65k-85k€ / Freelance: 450-650€ TJM / International: $80k-$130k";
    
    // Preferred cold-outreach language: "Auto", "English", "French"
    public string PreferredPitchLanguage { get; set; } = "Auto";
    
    // Raw CV text or Markdown content
    public string ResumeText { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public List<JobMatch> Matches { get; set; } = new();
}

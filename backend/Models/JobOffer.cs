using System;
using System.Collections.Generic;

namespace HiddenJobsHunter.Api.Models;

public class JobOffer
{
    public int Id { get; set; }
    
    // Unique identifier from source (e.g. "hn_41234567" or "remotive_12345")
    public required string ExternalId { get; set; }
    
    // Source: HackerNews, Remotive, Jobicy, Arbeitnow, CustomAts
    public required string Source { get; set; }
    
    public required string Title { get; set; }
    public required string Company { get; set; }
    public string? CompanyUrl { get; set; }
    
    public string Location { get; set; } = "Remote";
    
    // Worldwide, Europe, France, US-Only, Unknown
    public string RemoteScope { get; set; } = "Worldwide";
    
    // Whether this job can be performed from France (e.g. Worldwide, Europe/CET, B2B Contractor)
    public bool IsFranceCompatible { get; set; } = true;
    
    // CDI/Full-Time, Freelance/Contractor, Part-Time, Any
    public string ContractType { get; set; } = "Any";
    
    public string? Salary { get; set; }
    
    // Email (especially common in Hacker News) or direct contact name
    public string? DirectContact { get; set; }
    
    public string? ApplyUrl { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    // Comma-separated tech keywords: .NET, React, TypeScript, C#, Azure, etc.
    public string Tags { get; set; } = string.Empty;
    
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Discovered, Saved, Applied, Interview, Offer, Archived, Rejected
    public string Status { get; set; } = "Discovered";
    
    public string? UserNotes { get; set; }
    
    // Navigation property for matches across different profiles
    public List<JobMatch> Matches { get; set; } = new();
}

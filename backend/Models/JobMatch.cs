using System;

namespace HiddenJobsHunter.Api.Models;

public class JobMatch
{
    public int Id { get; set; }
    
    public int JobOfferId { get; set; }
    public JobOffer? JobOffer { get; set; }
    
    public int UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }
    
    // Overall match score (0 - 100)
    public int MatchScore { get; set; }
    
    // Tech alignment score (0 - 100)
    public int TechStackFit { get; set; }
    
    // Remote & location eligibility score (0 - 100)
    public int RemoteEligibilityFit { get; set; }
    
    // Quick summary
    public string Summary { get; set; } = string.Empty;
    
    // JSON string list of key strong points
    public string StrengthsJson { get; set; } = "[]";
    
    // JSON string list of missing skills or questions to clarify
    public string MissingSkillsJson { get; set; } = "[]";
    
    // Administrative advice for working from France (B2B contractor vs EOR vs CDI)
    public string AdministrativeFeasibility { get; set; } = string.Empty;
    
    // Ready-to-use tailored cold outreach pitch
    public string TailoredPitch { get; set; } = string.Empty;
    
    public string AiProviderUsed { get; set; } = "Heuristic";
    
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

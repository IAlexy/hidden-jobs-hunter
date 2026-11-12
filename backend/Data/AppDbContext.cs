using System;
using Microsoft.EntityFrameworkCore;
using HiddenJobsHunter.Api.Models;

namespace HiddenJobsHunter.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<JobOffer> JobOffers => Set<JobOffer>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<JobMatch> JobMatches => Set<JobMatch>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JobOffer>(entity =>
        {
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.Source);
        });

        modelBuilder.Entity<JobMatch>(entity =>
        {
            entity.HasIndex(e => new { e.JobOfferId, e.UserProfileId }).IsUnique();
            entity.HasIndex(e => e.MatchScore);

            entity.HasOne(m => m.JobOffer)
                  .WithMany(j => j.Matches)
                  .HasForeignKey(m => m.JobOfferId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.UserProfile)
                  .WithMany(u => u.Matches)
                  .HasForeignKey(m => m.UserProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(e => e.IsActive);
        });
    }

    public void SeedDefaults()
    {
        if (!Settings.Any())
        {
            Settings.Add(new AppSettings
            {
                Id = 1,
                AiProvider = "Heuristic",
                AutoMatchOnSync = true,
                OnlyFranceCompatibleJobs = false
            });
            SaveChanges();
        }

        if (!UserProfiles.Any())
        {
            UserProfiles.Add(new UserProfile
            {
                Name = "Demo Profile (Fullstack .NET / React)",
                IsActive = true,
                Title = "Senior Fullstack Software Engineer (.NET / React)",
                Country = "France",
                JobCategory = "software-dev",
                YearsOfExperience = 5,
                PreferredContracts = "CDI,Freelance/Contractor",
                RemoteScopePreference = "Worldwide,Europe,France",
                CanWorkAsContractorB2B = true,
                PrimarySkills = ".NET, C#, ASP.NET Core, React, TypeScript, SQL Server, PostgreSQL, REST APIs, Microservices",
                SecondarySkills = "Docker, Azure, CI/CD, Next.js, TailwindCSS, Redis, Entity Framework Core, Clean Architecture",
                Languages = "Français (natif), Anglais (courant / bilingue)",
                ExpectedSalaryOrTjm = "CDI: 65k-80k€ / TJM Freelance: 500-650€ / International: $80k-$130k",
                PreferredPitchLanguage = "Auto",
                ResumeText = @"Demo Candidate - Fullstack Software Engineer (.NET / C# / React / TypeScript)
Expérience : ~5 ans en développement logiciel d'applications web & API scalables.
Localisation : France (Télétravail à 100% / Full Remote).
Langues : Français (natif), Anglais (bilingue professionnel).
Statut envisageable : CDI (direct ou via EOR type Deel/Remote) OU Contrat Freelance / Contractor B2B (facturation directe internationale).

Points forts & compétences :
- Backend : C#, .NET 8 / .NET Core, ASP.NET Core Web API, EF Core, architecture hexagonale / Clean Architecture, DDD, microservices, background workers.
- Frontend : React, TypeScript, Next.js, Tailwind CSS, Redux Toolkit, React Query, UI/UX réactive et performante.
- Bases de données : PostgreSQL, SQL Server, Redis, optimisation de requêtes et migrations.
- DevOps & Cloud : Docker, Azure, GitHub Actions, CI/CD, monitoring et logs.
- Méthodologie : Agile/Scrum, tests unitaires, revue de code rigoureuse, autonomie forte en contexte remote distribué."
            });
            SaveChanges();
        }
    }
}

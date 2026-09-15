using CvManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AttributeCategory> AttributeCategories => Set<AttributeCategory>();

    public DbSet<AttributeDefinition> AttributeDefinitions =>
        Set<AttributeDefinition>();

    public DbSet<AttributeOption> AttributeOptions =>
        Set<AttributeOption>();

    public DbSet<CandidateProfile> CandidateProfiles =>
        Set<CandidateProfile>();

    public DbSet<CandidateAttributeValue> CandidateAttributeValues =>
        Set<CandidateAttributeValue>();

    public DbSet<Project> Projects =>
        Set<Project>();

    public DbSet<TechnologyTag> TechnologyTags =>
        Set<TechnologyTag>();

    public DbSet<ProjectTechnologyTag> ProjectTechnologyTags =>
        Set<ProjectTechnologyTag>();

    public DbSet<Position> Positions =>
        Set<Position>();

    public DbSet<PositionAttribute> PositionAttributes =>
        Set<PositionAttribute>();

    public DbSet<PositionProjectTag> PositionProjectTags =>
        Set<PositionProjectTag>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // =========================================================
        // Attribute Categories
        // =========================================================

        builder.Entity<AttributeCategory>()
            .HasIndex(x => x.Name)
            .IsUnique();

        // =========================================================
        // Attribute Definitions
        // =========================================================

        builder.Entity<AttributeDefinition>()
            .HasIndex(x => x.Name)
            .IsUnique();

        builder.Entity<AttributeDefinition>()
            .Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Entity<AttributeDefinition>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Attributes)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================================================
        // Attribute Options
        // =========================================================

        builder.Entity<AttributeOption>()
            .HasOne(x => x.AttributeDefinition)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================================================
        // Candidate Profile ↔ ApplicationUser
        // =========================================================

        builder.Entity<CandidateProfile>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        builder.Entity<CandidateProfile>()
            .HasOne(x => x.User)
            .WithOne(x => x.CandidateProfile)
            .HasForeignKey<CandidateProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CandidateProfile>()
            .Property(x => x.Version)
            .IsConcurrencyToken();

        // =========================================================
        // Candidate Attribute Values
        // =========================================================

        builder.Entity<CandidateAttributeValue>()
            .HasIndex(x => new
            {
                x.CandidateProfileId,
                x.AttributeDefinitionId
            })
            .IsUnique();

        builder.Entity<CandidateAttributeValue>()
            .HasOne(x => x.CandidateProfile)
            .WithMany(x => x.AttributeValues)
            .HasForeignKey(x => x.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CandidateAttributeValue>()
            .HasOne(x => x.AttributeDefinition)
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CandidateAttributeValue>()
            .Property(x => x.Version)
            .IsConcurrencyToken();

        // =========================================================
// Position Access Rules
// =========================================================

builder.Entity<PositionAccessRule>()
    .HasOne(x => x.Position)
    .WithMany()
    .HasForeignKey(x => x.PositionId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<PositionAccessRule>()
    .HasOne(x => x.AttributeDefinition)
    .WithMany()
    .HasForeignKey(x => x.AttributeDefinitionId)
    .OnDelete(DeleteBehavior.Restrict);

// =========================================================
// CV
// =========================================================

builder.Entity<Cv>()
    .HasOne(x => x.CandidateProfile)
    .WithMany()
    .HasForeignKey(x => x.CandidateProfileId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<Cv>()
    .HasOne(x => x.Position)
    .WithMany()
    .HasForeignKey(x => x.PositionId)
    .OnDelete(DeleteBehavior.Restrict);

builder.Entity<Cv>()
    .HasIndex(x => new
    {
        x.CandidateProfileId,
        x.PositionId
    })
    .IsUnique();

builder.Entity<Cv>()
    .Property(x => x.Version)
    .IsConcurrencyToken();

builder.Entity<Cv>()
    .HasOne(x => x.CandidateProfile)
    .WithMany(x => x.Cvs)
    .HasForeignKey(x => x.CandidateProfileId)
    .OnDelete(DeleteBehavior.Cascade);

// =========================================================
// CV Attribute Values
// =========================================================

builder.Entity<CvAttributeValue>()
    .HasIndex(x => new
    {
        x.CvId,
        x.AttributeDefinitionId
    })
    .IsUnique();

builder.Entity<CvAttributeValue>()
    .HasOne(x => x.Cv)
    .WithMany(x => x.AttributeValues)
    .HasForeignKey(x => x.CvId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<CvAttributeValue>()
    .HasOne(x => x.AttributeDefinition)
    .WithMany()
    .HasForeignKey(x => x.AttributeDefinitionId)
    .OnDelete(DeleteBehavior.Restrict);

builder.Entity<CvAttributeValue>()
    .Property(x => x.Version)
    .IsConcurrencyToken();

// =========================================================
// CV Projects
// =========================================================

builder.Entity<CvProject>()
    .HasKey(x => new
    {
        x.CvId,
        x.ProjectId
    });

builder.Entity<CvProject>()
    .HasOne(x => x.Cv)
    .WithMany(x => x.Projects)
    .HasForeignKey(x => x.CvId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<CvProject>()
    .HasOne(x => x.Project)
    .WithMany()
    .HasForeignKey(x => x.ProjectId)
    .OnDelete(DeleteBehavior.Restrict);

// =========================================================
// CV Likes
// =========================================================

builder.Entity<CvLike>()
    .HasIndex(x => new
    {
        x.CvId,
        x.RecruiterId
    })
    .IsUnique();

builder.Entity<CvLike>()
    .HasOne(x => x.Cv)
    .WithMany()
    .HasForeignKey(x => x.CvId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<CvLike>()
    .HasOne(x => x.Recruiter)
    .WithMany()
    .HasForeignKey(x => x.RecruiterId)
    .OnDelete(DeleteBehavior.Restrict);

// =========================================================
// Position Discussions
// =========================================================

builder.Entity<PositionDiscussion>()
    .HasOne(x => x.Position)
    .WithMany()
    .HasForeignKey(x => x.PositionId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<PositionDiscussion>()
    .HasOne(x => x.Author)
    .WithMany()
    .HasForeignKey(x => x.AuthorId)
    .OnDelete(DeleteBehavior.Restrict);   

        // =========================================================
        // Projects
        // =========================================================

        builder.Entity<Project>()
            .HasOne(x => x.CandidateProfile)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================================================
        // Technology Tags
        // =========================================================

        builder.Entity<TechnologyTag>()
            .HasIndex(x => x.Name)
            .IsUnique();

        builder.Entity<ProjectTechnologyTag>()
            .HasKey(x => new
            {
                x.ProjectId,
                x.TechnologyTagId
            });

        builder.Entity<ProjectTechnologyTag>()
            .HasOne(x => x.Project)
            .WithMany(x => x.TechnologyTags)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectTechnologyTag>()
            .HasOne(x => x.TechnologyTag)
            .WithMany(x => x.ProjectTags)
            .HasForeignKey(x => x.TechnologyTagId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================================================
        // Positions
        // =========================================================

        builder.Entity<Position>()
            .Property(x => x.Version)
            .IsConcurrencyToken();

        // =========================================================
        // Position ↔ AttributeDefinition
        // =========================================================

        builder.Entity<PositionAttribute>()
            .HasKey(x => new
            {
                x.PositionId,
                x.AttributeDefinitionId
            });

        builder.Entity<PositionAttribute>()
            .HasOne(x => x.Position)
            .WithMany(x => x.Attributes)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PositionAttribute>()
            .HasOne(x => x.AttributeDefinition)
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================================================
        // Position ↔ TechnologyTag
        // =========================================================

        builder.Entity<PositionProjectTag>()
            .HasKey(x => new
            {
                x.PositionId,
                x.TechnologyTagId
            });

        builder.Entity<PositionProjectTag>()
            .HasOne(x => x.Position)
            .WithMany(x => x.ProjectTags)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PositionProjectTag>()
            .HasOne(x => x.TechnologyTag)
            .WithMany()
            .HasForeignKey(x => x.TechnologyTagId)
            .OnDelete(DeleteBehavior.Restrict);
    }


public DbSet<PositionAccessRule> PositionAccessRules =>
    Set<PositionAccessRule>();

public DbSet<Cv> Cvs =>
    Set<Cv>();

public DbSet<CvAttributeValue> CvAttributeValues =>
    Set<CvAttributeValue>();

public DbSet<CvProject> CvProjects =>
    Set<CvProject>();

public DbSet<CvLike> CvLikes =>
    Set<CvLike>();

public DbSet<PositionDiscussion> PositionDiscussions =>
    Set<PositionDiscussion>();
}
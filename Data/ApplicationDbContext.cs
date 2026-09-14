using CvManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AttributeCategory> AttributeCategories =>
        Set<AttributeCategory>();

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

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AttributeCategory>()
            .HasIndex(x => x.Name)
            .IsUnique();

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

        builder.Entity<AttributeOption>()
            .HasOne(x => x.AttributeDefinition)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Candidate profile ↔ ApplicationUser
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

        // Candidate attribute values
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

        // Projects
        builder.Entity<Project>()
            .HasOne(x => x.CandidateProfile)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Technology tags
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
    }
}
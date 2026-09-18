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


    // =========================================================
    // Attribute Library
    // =========================================================

    public DbSet<AttributeCategory>
        AttributeCategories =>
        Set<AttributeCategory>();

    public DbSet<AttributeDefinition>
        AttributeDefinitions =>
        Set<AttributeDefinition>();

    public DbSet<AttributeOption>
        AttributeOptions =>
        Set<AttributeOption>();


    // =========================================================
    // Candidate Profile
    // =========================================================

    public DbSet<CandidateProfile>
        CandidateProfiles =>
        Set<CandidateProfile>();

    public DbSet<CandidateAttributeValue>
        CandidateAttributeValues =>
        Set<CandidateAttributeValue>();


    // =========================================================
    // Projects
    // =========================================================

    public DbSet<Project>
        Projects =>
        Set<Project>();

    public DbSet<TechnologyTag>
        TechnologyTags =>
        Set<TechnologyTag>();

    public DbSet<ProjectTechnologyTag>
        ProjectTechnologyTags =>
        Set<ProjectTechnologyTag>();


    // =========================================================
    // Positions
    // =========================================================

    public DbSet<Position>
        Positions =>
        Set<Position>();

    public DbSet<PositionAttribute>
        PositionAttributes =>
        Set<PositionAttribute>();

    public DbSet<PositionProjectTag>
        PositionProjectTags =>
        Set<PositionProjectTag>();

    public DbSet<PositionAccessRule>
        PositionAccessRules =>
        Set<PositionAccessRule>();


    // =========================================================
    // CVs
    // =========================================================

    public DbSet<Cv>
        Cvs =>
        Set<Cv>();

    public DbSet<CvAttributeValue>
        CvAttributeValues =>
        Set<CvAttributeValue>();

    public DbSet<CvProject>
        CvProjects =>
        Set<CvProject>();

    public DbSet<CvLike>
        CvLikes =>
        Set<CvLike>();


    // =========================================================
    // Discussions
    // =========================================================

    public DbSet<PositionDiscussion>
        PositionDiscussions =>
        Set<PositionDiscussion>();


    // =========================================================
    // MODEL CONFIGURATION
    // =========================================================

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // =====================================================
        // Attribute Category
        // =====================================================

        modelBuilder.Entity<AttributeCategory>(
            entity =>
            {
                entity
                    .HasIndex(x => x.Name)
                    .IsUnique();

                entity
                    .HasMany(x => x.Attributes)
                    .WithOne(x => x.Category)
                    .HasForeignKey(x =>
                        x.CategoryId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });


        // =====================================================
        // Attribute Definition
        // =====================================================

        modelBuilder.Entity<AttributeDefinition>(
            entity =>
            {
                entity
                    .HasIndex(x => x.Name)
                    .IsUnique();

                entity
                    .Property(x => x.Version)
                    .IsConcurrencyToken();

                entity
                    .HasMany(x => x.Options)
                    .WithOne(x => x.AttributeDefinition)
                    .HasForeignKey(x =>
                        x.AttributeDefinitionId)
                    .OnDelete(
                        DeleteBehavior.Cascade);
            });


        // =====================================================
        // Candidate Profile
        // =====================================================

        modelBuilder.Entity<CandidateProfile>(
            entity =>
            {
                entity
                    .HasIndex(x => x.UserId)
                    .IsUnique();

                entity
                    .Property(x => x.Version)
                    .IsConcurrencyToken();

                entity
                    .HasOne(x => x.User)
                    .WithOne(x =>
                        x.CandidateProfile)
                    .HasForeignKey<CandidateProfile>(
                        x => x.UserId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity
                    .HasMany(x => x.AttributeValues)
                    .WithOne(x =>
                        x.CandidateProfile)
                    .HasForeignKey(x =>
                        x.CandidateProfileId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity
                    .HasMany(x => x.Projects)
                    .WithOne(x =>
                        x.CandidateProfile)
                    .HasForeignKey(x =>
                        x.CandidateProfileId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity
                    .HasMany(x => x.Cvs)
                    .WithOne(x =>
                        x.CandidateProfile)
                    .HasForeignKey(x =>
                        x.CandidateProfileId)
                    .OnDelete(
                        DeleteBehavior.Cascade);
            });


        // =====================================================
        // Candidate Attribute Values
        // =====================================================

        modelBuilder.Entity<CandidateAttributeValue>(
            entity =>
            {
                entity
                    .HasIndex(x =>
                        new
                        {
                            x.CandidateProfileId,
                            x.AttributeDefinitionId
                        })
                    .IsUnique();

                entity
                    .Property(x => x.Version)
                    .IsConcurrencyToken();

                entity
                    .HasOne(x =>
                        x.AttributeDefinition)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.AttributeDefinitionId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });


        // =====================================================
        // Technology Tag
        // =====================================================

        modelBuilder.Entity<TechnologyTag>(
            entity =>
            {
                entity
                    .HasIndex(x => x.Name)
                    .IsUnique();
            });


        // =====================================================
        // Project Technology Tags
        // =====================================================

        modelBuilder.Entity<ProjectTechnologyTag>(
            entity =>
            {
                entity
                    .HasKey(
                        x =>
                            new
                            {
                                x.ProjectId,
                                x.TechnologyTagId
                            });

                entity
                    .HasOne(x => x.Project)
                    .WithMany(x =>
                        x.TechnologyTags)
                    .HasForeignKey(x =>
                        x.ProjectId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity
                    .HasOne(x =>
                        x.TechnologyTag)
                    .WithMany(x =>
                        x.ProjectTags)
                    .HasForeignKey(x =>
                        x.TechnologyTagId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });


        // =====================================================
        // Position
        // =====================================================

        modelBuilder.Entity<Position>(
            entity =>
            {
                entity
                    .Property(x => x.Version)
                    .IsConcurrencyToken();


                // PostgreSQL full-text search
                //
                // SearchVector is a stored generated tsvector
                // built from Title + Description.
                entity
                    .HasGeneratedTsVectorColumn(
                        x =>
                            x.SearchVector,
                        "simple",
                        x =>
                            new
                            {
                                x.Title,
                                x.Description
                            })
                    .HasIndex(
                        x =>
                            x.SearchVector)
                    .HasMethod("GIN");


                entity
                    .HasMany(x =>
                        x.Attributes)
                    .WithOne(x =>
                        x.Position)
                    .HasForeignKey(x =>
                        x.PositionId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity
                    .HasMany(x =>
                        x.ProjectTags)
                    .WithOne(x =>
                        x.Position)
                    .HasForeignKey(x =>
                        x.PositionId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity
                    .HasMany(x =>
                        x.AccessRules)
                    .WithOne(x =>
                        x.Position)
                    .HasForeignKey(x =>
                        x.PositionId)
                    .OnDelete(
                        DeleteBehavior.Cascade);
            });


        // =====================================================
        // Position Attributes
        // =====================================================

        modelBuilder.Entity<PositionAttribute>(
            entity =>
            {
                entity
                    .HasKey(
                        x =>
                            new
                            {
                                x.PositionId,
                                x.AttributeDefinitionId
                            });

                entity
                    .HasOne(x =>
                        x.AttributeDefinition)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.AttributeDefinitionId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });


        // =====================================================
        // Position Project Tags
        // =====================================================

        modelBuilder.Entity<PositionProjectTag>(
            entity =>
            {
                entity
                    .HasKey(
                        x =>
                            new
                            {
                                x.PositionId,
                                x.TechnologyTagId
                            });

                entity
                    .HasOne(x =>
                        x.Position)
                    .WithMany(x =>
                        x.ProjectTags)
                    .HasForeignKey(x =>
                        x.PositionId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity
                    .HasOne(x =>
                        x.TechnologyTag)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.TechnologyTagId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });


        // =====================================================
        // Position Access Rules
        // =====================================================

        modelBuilder.Entity<PositionAccessRule>(
            entity =>
            {
                entity
                    .HasOne(x =>
                        x.Position)
                    .WithMany(x =>
                        x.AccessRules)
                    .HasForeignKey(x =>
                        x.PositionId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity
                    .HasOne(x =>
                        x.AttributeDefinition)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.AttributeDefinitionId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });


        // =====================================================
        // CV
        // =====================================================

        modelBuilder.Entity<Cv>(
            entity =>
            {
                entity
                    .HasIndex(
                        x =>
                            new
                            {
                                x.CandidateProfileId,
                                x.PositionId
                            })
                    .IsUnique();


                entity
                    .Property(x => x.Version)
                    .IsConcurrencyToken();


                // PostgreSQL full-text search
                //
                // CV title is indexed in a generated tsvector.
                entity
                    .HasGeneratedTsVectorColumn(
                        x =>
                            x.SearchVector,
                        "simple",
                        x =>
                            x.Title)
                    .HasIndex(
                        x =>
                            x.SearchVector)
                    .HasMethod("GIN");


                entity
                    .HasOne(x =>
                        x.Position)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.PositionId)
                    .OnDelete(
                        DeleteBehavior.Restrict);


                entity
                    .HasMany(x =>
                        x.AttributeValues)
                    .WithOne(x =>
                        x.Cv)
                    .HasForeignKey(x =>
                        x.CvId)
                    .OnDelete(
                        DeleteBehavior.Cascade);


                entity
                    .HasMany(x =>
                        x.Projects)
                    .WithOne(x =>
                        x.Cv)
                    .HasForeignKey(x =>
                        x.CvId)
                    .OnDelete(
                        DeleteBehavior.Cascade);
            });


        // =====================================================
        // CV Attribute Values
        // =====================================================

        modelBuilder.Entity<CvAttributeValue>(
            entity =>
            {
                entity
                    .HasOne(x =>
                        x.Cv)
                    .WithMany(x =>
                        x.AttributeValues)
                    .HasForeignKey(x =>
                        x.CvId)
                    .OnDelete(
                        DeleteBehavior.Cascade);


                entity
                    .HasOne(x =>
                        x.AttributeDefinition)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.AttributeDefinitionId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });


        // =====================================================
        // CV Projects
        // =====================================================

        modelBuilder.Entity<CvProject>(
            entity =>
            {
                entity
                    .HasKey(
                        x =>
                            new
                            {
                                x.CvId,
                                x.ProjectId
                            });


                entity
                    .HasOne(x =>
                        x.Cv)
                    .WithMany(x =>
                        x.Projects)
                    .HasForeignKey(x =>
                        x.CvId)
                    .OnDelete(
                        DeleteBehavior.Cascade);


                entity
                    .HasOne(x =>
                        x.Project)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.ProjectId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });


        // =====================================================
        // CV Likes
        // =====================================================

        modelBuilder.Entity<CvLike>(
            entity =>
            {
                entity
                    .HasIndex(
                        x =>
                            new
                            {
                                x.CvId,
                                x.RecruiterId
                            })
                    .IsUnique();


                entity
                    .HasOne(x =>
                        x.Cv)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.CvId)
                    .OnDelete(
                        DeleteBehavior.Cascade);


                entity
                    .HasOne(x =>
                        x.Recruiter)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.RecruiterId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });


        // =====================================================
        // Position Discussions
        // =====================================================

        modelBuilder.Entity<PositionDiscussion>(
            entity =>
            {
                entity
                    .HasOne(x =>
                        x.Position)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.PositionId)
                    .OnDelete(
                        DeleteBehavior.Cascade);


                entity
                    .HasOne(x =>
                        x.Author)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.AuthorId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });
    }
}
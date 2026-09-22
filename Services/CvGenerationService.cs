using CvManagementSystem.Data;
using CvManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Services;

public class CvGenerationService
{
    private readonly ApplicationDbContext _context;
    private readonly PositionAccessService _positionAccessService;

    public CvGenerationService(
        ApplicationDbContext context,
        PositionAccessService positionAccessService)
    {
        _context = context;
        _positionAccessService =
            positionAccessService;
    }

    public async Task<Cv?> GenerateAsync(
        int positionId,
        string userId)
    {
        var position =
            await _context.Positions
                .AsNoTracking()
                .Include(x => x.Attributes)
                    .ThenInclude(
                        x => x.AttributeDefinition)
                .Include(x => x.ProjectTags)
                    .ThenInclude(
                        x => x.TechnologyTag)
                .Include(x => x.AccessRules)
                    .ThenInclude(
                        x => x.AttributeDefinition)
                .FirstOrDefaultAsync(
                    x => x.Id == positionId);

        if (position == null)
        {
            return null;
        }

        var profile =
            await _context.CandidateProfiles
                .Include(x => x.AttributeValues)
                // Resume sections are profile-owned rather than copied into
                // each CV. CV Details assembles them with the position's
                // selected attributes and projects, so later profile edits
                // are reflected without losing position-specific filtering.
                .Include(x => x.EducationEntries)
                .Include(x => x.WorkExperiences)
                .Include(x => x.Projects)
                    .ThenInclude(
                        x => x.TechnologyTags)
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);

        if (profile == null)
        {
            return null;
        }

        if (!PositionAccessService.IsAuthorized(
                position,
                profile))
        {
            // The controller turns a null result into a helpful message.
            // Throwing here produced a server error for a stale client page
            // after access rules had changed.
            return null;
        }

        var existing =
            await _context.Cvs
                .FirstOrDefaultAsync(
                    x =>
                        x.CandidateProfileId ==
                        profile.Id &&
                        x.PositionId ==
                        position.Id);

        if (existing != null)
        {
            return existing;
        }

        var cv =
            new Cv
            {
                CandidateProfileId =
                    profile.Id,

                PositionId =
                    position.Id,

                Title =
                    $"{position.Title} CV",

                IsPublished =
                    false,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    DateTime.UtcNow,

                Version =
                    Guid.NewGuid()
            };

        // Persist a complete snapshot for searching and backwards-compatible
        // CV data. The Details page also reads the live profile so later
        // additions are immediately available on existing CVs.
        foreach (var profileValue in profile.AttributeValues
            .GroupBy(x => x.AttributeDefinitionId)
            .Select(x => x.First())
            .OrderBy(x => x.AttributeDefinitionId))
        {
            cv.AttributeValues.Add(
                new CvAttributeValue
                {
                    Cv = cv,

                    AttributeDefinitionId =
                        profileValue.AttributeDefinitionId,

                    Value = profileValue.Value,

                    UpdatedAt =
                        DateTime.UtcNow,

                    Version =
                        Guid.NewGuid()
                });
        }

        // Positions may restrict a generated CV to projects tagged with their
        // configured technologies, capped at MaxProjects. An untagged position
        // (no ProjectTags configured) imposes no relevance filter, only the cap.
        var positionTagIds =
            position.ProjectTags
                .Select(x => x.TechnologyTagId)
                .ToHashSet();

        var projectCandidates =
            profile.Projects
                .Where(x =>
                    positionTagIds.Count == 0 ||
                    x.TechnologyTags.Any(t =>
                        positionTagIds.Contains(t.TechnologyTagId)))
                .OrderByDescending(x => x.EndDate ?? DateOnly.MaxValue)
                .ThenByDescending(x => x.StartDate)
                .Take(position.MaxProjects)
                .ToList();

        for (var i = 0;
             i < projectCandidates.Count;
             i++)
        {
            cv.Projects.Add(
                new CvProject
                {
                    Cv = cv,

                    ProjectId = projectCandidates[i].Id,

                    SortOrder = i
                });
        }

        _context.Cvs.Add(cv);

        await _context.SaveChangesAsync();

        return cv;
    }
}

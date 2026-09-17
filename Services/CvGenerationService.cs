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
            throw new UnauthorizedAccessException(
                "The candidate is not currently authorized " +
                "to create a CV for this position.");
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

        var profileValues = profile.AttributeValues
          .GroupBy(x => x.AttributeDefinitionId)
          .ToDictionary(
             x => x.Key,
             x => x.FirstOrDefault()?.Value);

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

        foreach (
            var positionAttribute
            in position.Attributes
                .OrderBy(x => x.SortOrder))
        {
            profileValues.TryGetValue(
                positionAttribute
                    .AttributeDefinitionId,
                out var value);

            cv.AttributeValues.Add(
                new CvAttributeValue
                {
                    Cv = cv,

                    AttributeDefinitionId =
                        positionAttribute
                            .AttributeDefinitionId,

                    Value =
                        value,

                    UpdatedAt =
                        DateTime.UtcNow,

                    Version =
                        Guid.NewGuid()
                });
        }

        var positionTagIds =
            position.ProjectTags
                .Select(x => x.TechnologyTagId)
                .ToHashSet();

        var projectCandidates =
            profile.Projects
                .Select(
                    project =>
                    {
                        var matchingTags =
                            project.TechnologyTags
                                .Count(
                                    tag =>
                                        positionTagIds.Contains(
                                            tag.TechnologyTagId));

                        return new
                        {
                            Project = project,
                            MatchCount = matchingTags
                        };
                    })
                .Where(
                    x =>
                        positionTagIds.Count == 0 ||
                        x.MatchCount > 0)
                .OrderByDescending(
                    x => x.MatchCount)
                .ThenByDescending(
                    x => x.Project.StartDate)
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

                    ProjectId =
                        projectCandidates[i]
                            .Project.Id,

                    SortOrder = i
                });
        }

        _context.Cvs.Add(cv);

        await _context.SaveChangesAsync();

        return cv;
    }
}
using CvManagementSystem.Data;
using CvManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Services;

/// <summary>
/// Computes the "badges" / "achievements" optional feature: simple,
/// deterministic milestones derived from data that already exists
/// (projects, CVs, likes received), with no extra tables required.
/// </summary>
public class BadgeService
{
    private readonly ApplicationDbContext _context;

    public BadgeService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Each tier is (threshold, code, label, icon, color). Icons are drawn
    // inline as SVG paths, not raster images, so nothing is uploaded to
    // storage and the panel stays a single small self-contained file.
    private static readonly (int Threshold, string Code, string Label)[]
        ProjectTiers =
        {
            (1, "project-1", "First project"),
            (5, "project-5", "5 projects"),
            (10, "project-10", "10 projects")
        };

    private static readonly (int Threshold, string Code, string Label)[]
        CvTiers =
        {
            (1, "cv-1", "First CV"),
            (5, "cv-5", "5 CVs"),
            (10, "cv-10", "10 CVs")
        };

    private static readonly (int Threshold, string Code, string Label)[]
        LikeTiers =
        {
            (1, "like-1", "First like"),
            (10, "like-10", "10 likes"),
            (25, "like-25", "25 likes"),
            (50, "like-50", "50 likes")
        };

    public async Task<BadgeSummaryViewModel> GetBadgesAsync(int candidateProfileId)
    {
        var projectCount =
            await _context.Projects
                .AsNoTracking()
                .CountAsync(x => x.CandidateProfileId == candidateProfileId);

        var publishedCvCount =
            await _context.Cvs
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.CandidateProfileId == candidateProfileId &&
                        x.IsPublished);

        var likeCount =
            await _context.CvLikes
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.Cv.CandidateProfileId == candidateProfileId);

        var earned = new List<BadgeViewModel>();

        AddHighestEarnedTier(earned, ProjectTiers, projectCount, "project", projectCount);
        AddHighestEarnedTier(earned, CvTiers, publishedCvCount, "cv", publishedCvCount);
        AddHighestEarnedTier(earned, LikeTiers, likeCount, "like", likeCount);

        return new BadgeSummaryViewModel
        {
            ProjectCount = projectCount,
            PublishedCvCount = publishedCvCount,
            LikeCount = likeCount,
            Earned = earned
        };
    }

    // Only the highest tier reached per category is shown (earning "10
    // projects" implies "5 projects" and "first project").
    private static void AddHighestEarnedTier(
        List<BadgeViewModel> earned,
        (int Threshold, string Code, string Label)[] tiers,
        int count,
        string category,
        int currentValue)
    {
        var highest = tiers
            .Where(t => count >= t.Threshold)
            .OrderByDescending(t => t.Threshold)
            .FirstOrDefault();

        if (highest.Code != null)
        {
            earned.Add(new BadgeViewModel
            {
                Category = category,
                Code = highest.Code,
                Label = highest.Label,
                Value = currentValue
            });
        }
    }
}
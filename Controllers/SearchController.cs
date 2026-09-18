using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers;

public class SearchController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PositionAccessService _positionAccessService;

    public SearchController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        PositionAccessService positionAccessService)
    {
        _context = context;
        _userManager = userManager;
        _positionAccessService = positionAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var query = q?.Trim() ?? string.Empty;

        var user =
            await _userManager.GetUserAsync(User);

        var isCandidate =
            user != null &&
            await _userManager.IsInRoleAsync(
                user,
                "Candidate");

        var isRecruiter =
            user != null &&
            await _userManager.IsInRoleAsync(
                user,
                "Recruiter");

        var isAdministrator =
            user != null &&
            await _userManager.IsInRoleAsync(
                user,
                "Administrator");

        var visiblePositions =
            await _positionAccessService
                .GetVisiblePositionsAsync(
                    user?.Id,
                    isRecruiter || isAdministrator);

        var visiblePositionIds =
            visiblePositions
                .Select(x => x.Id)
                .ToHashSet();

        var model =
            new GlobalSearchViewModel
            {
                Query = query,

                CanSeeCvs =
                    isCandidate ||
                    isRecruiter ||
                    isAdministrator
            };

        if (string.IsNullOrWhiteSpace(query))
        {
            return View(model);
        }

        // =====================================================
        // Position full-text search
        // =====================================================

        var positionSearchQuery =
            EF.Functions.WebSearchToTsQuery(
                "simple",
                query);

        model.Positions =
    await _context.Positions
        .AsNoTracking()
       .Where(x => visiblePositionIds.Contains(x.Id))
        .Where(x =>
            x.SearchVector.Matches(
                EF.Functions.WebSearchToTsQuery("simple", query)))
        .OrderByDescending(x => x.UpdatedAt)
        .Take(50)
        .Select(x => new GlobalSearchPositionViewModel
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            IsPublic = x.IsPublic,
            UpdatedAt = x.UpdatedAt
        })
        .ToListAsync();


        // =====================================================
        // Submitted CV counts
        // =====================================================

        if (model.Positions.Count > 0)
        {
            var positionIds =
                model.Positions
                    .Select(x => x.Id)
                    .ToList();

            var submittedCounts =
                await _context.Cvs
                    .AsNoTracking()
                    .Where(x =>
                        x.IsPublished &&
                        positionIds.Contains(
                            x.PositionId))
                    .GroupBy(x =>
                        x.PositionId)
                    .Select(x =>
                        new
                        {
                            PositionId = x.Key,
                            Count = x.Count()
                        })
                    .ToDictionaryAsync(
                        x => x.PositionId,
                        x => x.Count);

            foreach (var position
                     in model.Positions)
            {
                position.SubmittedCvCount =
                    submittedCounts.GetValueOrDefault(
                        position.Id);
            }
        }


        // =====================================================
        // CV search
        // =====================================================

        if (isCandidate ||
            isRecruiter ||
            isAdministrator)
        {
            var cvQuery =
                _context.Cvs
                    .AsNoTracking()
                    .AsQueryable();


            if (isCandidate)
            {
                cvQuery =
                    cvQuery.Where(x =>
                        x.CandidateProfile.UserId ==
                        user!.Id &&
                        visiblePositionIds.Contains(
                            x.PositionId));
            }
            else if (isRecruiter)
            {
                cvQuery =
                    cvQuery.Where(x =>
                        x.IsPublished &&
                        visiblePositionIds.Contains(
                            x.PositionId));
            }
            // Administrator sees all CVs.


            var cvSearchQuery =
                EF.Functions.WebSearchToTsQuery(
                    "simple",
                    query);


            cvQuery = cvQuery.Where(x =>
    x.SearchVector.Matches(
        EF.Functions.WebSearchToTsQuery("simple", query))
    || EF.Functions.ILike(
        x.Position.Title,
        "%" + query + "%")
    || EF.Functions.ILike(
        x.CandidateProfile.FirstName,
        "%" + query + "%")
    || EF.Functions.ILike(
        x.CandidateProfile.LastName,
        "%" + query + "%")
    || x.CandidateProfile.AttributeValues.Any(av =>
        EF.Functions.ILike(av.Value ?? string.Empty, "%" + query + "%"))
    || x.CandidateProfile.Projects.Any(p =>
        EF.Functions.ILike(p.Name, "%" + query + "%"))
);


            model.Cvs =
                await cvQuery
                    .OrderByDescending(
                        x => x.UpdatedAt)
                    .Take(50)
                    .Select(x =>
                        new GlobalSearchCvViewModel
                        {
                            Id = x.Id,

                            Title = x.Title,

                            PositionId =
                                x.PositionId,

                            PositionTitle =
                                x.Position.Title,

                            CandidateName =
                                (
                                    x.CandidateProfile.FirstName +
                                    " " +
                                    x.CandidateProfile.LastName
                                ).Trim(),

                            IsPublished =
                                x.IsPublished,

                            LikeCount =
                                _context.CvLikes.Count(
                                    like =>
                                        like.CvId ==
                                        x.Id),

                            UpdatedAt =
                                x.UpdatedAt
                        })
                    .ToListAsync();
        }


        return View(model);
    }
}
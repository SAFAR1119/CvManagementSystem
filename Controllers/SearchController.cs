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

        var user = await _userManager.GetUserAsync(User);

        var isAuthenticated = user != null;

        var isRecruiter =
            isAuthenticated &&
            await _userManager.IsInRoleAsync(
                user!,
                "Recruiter");

        var isAdministrator =
            isAuthenticated &&
            await _userManager.IsInRoleAsync(
                user!,
                "Administrator");

        var isCandidate =
            isAuthenticated &&
            await _userManager.IsInRoleAsync(
                user!,
                "Candidate");

        var visiblePositions =
            await _positionAccessService.GetVisiblePositionsAsync(
                user?.Id,
                isRecruiter || isAdministrator);

        var visiblePositionIds =
            visiblePositions
                .Select(x => x.Id)
                .ToHashSet();

        var model = new GlobalSearchViewModel
        {
            Query = query,
            CanSeeCvs = isRecruiter || isAdministrator || isCandidate
        };

        if (string.IsNullOrWhiteSpace(query))
        {
            return View(model);
        }

        /*
         * Position search.
         *
         * Search title and description only. Access filtering has
         * already been handled through PositionAccessService.
         */
        model.Positions = visiblePositions
            .Where(x =>
                x.Title.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ||
                (x.Description?.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(50)
            .Select(x => new GlobalSearchPositionViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                IsPublic = x.IsPublic,
                UpdatedAt = x.UpdatedAt
            })
            .ToList();

        /*
         * Only authenticated users may search CVs.
         *
         * Recruiters:
         *   published CVs only
         *
         * Candidates:
         *   their own CVs, subject to current position access
         *
         * Administrators:
         *   all CVs
         */
        if (isRecruiter || isAdministrator || isCandidate)
        {
            var cvQuery = _context.Cvs
                .AsNoTracking()
                .Include(x => x.Position)
                .Include(x => x.CandidateProfile)
                .AsQueryable();

            if (isRecruiter)
            {
                cvQuery = cvQuery.Where(x =>
                    x.IsPublished &&
                    visiblePositionIds.Contains(x.PositionId));
            }
            else if (isCandidate)
            {
                cvQuery = cvQuery.Where(x =>
                    x.CandidateProfile.UserId == user!.Id &&
                    visiblePositionIds.Contains(x.PositionId));
            }

            

            cvQuery = cvQuery.Where(x =>
                EF.Functions.ILike(
                    x.Title,
                    $"%{query}%") ||

                EF.Functions.ILike(
                    x.Position.Title,
                    $"%{query}%") ||

                EF.Functions.ILike(
                    x.CandidateProfile.FirstName,
                    $"%{query}%") ||

                EF.Functions.ILike(
                    x.CandidateProfile.LastName,
                    $"%{query}%") ||

                x.AttributeValues.Any(attribute =>
                    EF.Functions.ILike(
                        attribute.Value ?? "",
                        $"%{query}%")) ||

                x.Projects.Any(project =>
                    project.Project.Name.Contains(
                        query)));

            model.Cvs = await cvQuery
                .OrderByDescending(x => x.UpdatedAt)
                .Take(50)
                .Select(x => new GlobalSearchCvViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    PositionId = x.PositionId,
                    PositionTitle = x.Position.Title,
                    CandidateName =
                        x.CandidateProfile.FirstName +
                        " " +
                        x.CandidateProfile.LastName,
                    IsPublished = x.IsPublished,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();
        }

        /*
         * Submitted CV counts for matching positions.
         * One grouped query, reused for the result list.
         */
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
                        positionIds.Contains(x.PositionId))
                    .GroupBy(x => x.PositionId)
                    .Select(x => new
                    {
                        PositionId = x.Key,
                        Count = x.Count()
                    })
                    .ToDictionaryAsync(
                        x => x.PositionId,
                        x => x.Count);

            foreach (var position in model.Positions)
            {
                position.SubmittedCvCount =
                    submittedCounts.GetValueOrDefault(
                        position.Id);
            }
        }

        return View(model);
    }
}
using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PositionAccessService _positionAccessService;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        PositionAccessService positionAccessService)
    {
        _context = context;
        _userManager = userManager;
        _positionAccessService = positionAccessService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);

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

        var userId = user?.Id;

        /*
         * PositionAccessService handles:
         * - anonymous users -> public positions
         * - candidates -> public + currently authorized positions
         * - recruiters/admins -> all positions
         */
        var visiblePositions =
            await _positionAccessService.GetVisiblePositionsAsync(
                userId,
                isRecruiter || isAdministrator);

        var visiblePositionIds =
            visiblePositions
                .Select(x => x.Id)
                .ToHashSet();

        /*
         * Published CV counts are used as "submitted CVs".
         * This is loaded once and then reused in memory.
         */
        var cvCounts = await _context.Cvs
            .AsNoTracking()
            .Where(x =>
                x.IsPublished &&
                visiblePositionIds.Contains(x.PositionId))
            .GroupBy(x => x.PositionId)
            .Select(x => new
            {
                PositionId = x.Key,
                Count = x.Count()
            })
            .ToDictionaryAsync(
                x => x.PositionId,
                x => x.Count);

        var latestPositions = visiblePositions
            .OrderByDescending(x => x.UpdatedAt)
            .Take(10)
            .Select(x => new HomePositionViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                UpdatedAt = x.UpdatedAt,
                SubmittedCvCount =
                    cvCounts.GetValueOrDefault(x.Id),
                IsPublic = x.IsPublic
            })
            .ToList();

        var popularPositions = visiblePositions
            .OrderByDescending(
                x => cvCounts.GetValueOrDefault(x.Id))
            .ThenByDescending(x => x.UpdatedAt)
            .Take(5)
            .Select(x => new HomePositionViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                UpdatedAt = x.UpdatedAt,
                SubmittedCvCount =
                    cvCounts.GetValueOrDefault(x.Id),
                IsPublic = x.IsPublic
            })
            .ToList();

        List<HomeTagViewModel> technologyTags;

        if (isRecruiter || isAdministrator)
        {
            /*
             * Recruiters/admins see tags coming from published CV projects.
             * Those tags link to candidate CVs.
             */
            var recruiterTags = await _context.CvProjects
                .AsNoTracking()
                .Where(x => x.Cv.IsPublished)
                .SelectMany(x =>
                    x.Project.TechnologyTags
                        .Select(t => t.TechnologyTag.Name))
                .GroupBy(x => x)
                .Select(x => new
                {
                    Name = x.Key,
                    Count = x.Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Take(25)
                .ToListAsync();

            technologyTags = recruiterTags
                .Select(x => new HomeTagViewModel
                {
                    Name = x.Name,
                    Count = x.Count,
                    Url = Url.Action(
                        "Index",
                        "Cvs",
                        new { tag = x.Name }) ?? "#"
                })
                .ToList();
        }
        else
        {
            /*
             * Candidates/anonymous users see position technology tags.
             * Those tags link to position results.
             */
            var candidateTags = await _context.PositionProjectTags
                .AsNoTracking()
                .Where(x => visiblePositionIds.Contains(x.PositionId))
                .Select(x => x.TechnologyTag.Name)
                .GroupBy(x => x)
                .Select(x => new
                {
                    Name = x.Key,
                    Count = x.Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Take(25)
                .ToListAsync();

            technologyTags = candidateTags
                .Select(x => new HomeTagViewModel
                {
                    Name = x.Name,
                    Count = x.Count,
                    Url = Url.Action(
                        "Index",
                        "Positions",
                        new { tag = x.Name }) ?? "#"
                })
                .ToList();
        }

        /*
         * Anonymous users should only see public positions in the
         * position statistic. Authenticated users see the positions
         * available to their role.
         */
        var totalPositions =
            visiblePositions.Count;

        var newCvsLast24Hours = await _context.Cvs
            .AsNoTracking()
            .CountAsync(x =>
                x.CreatedAt >= DateTime.UtcNow.AddHours(-24));

        var totalSubmittedCvs = await _context.Cvs
            .AsNoTracking()
            .CountAsync(x => x.IsPublished);

        var candidateRoleId = await _context.Roles
            .AsNoTracking()
            .Where(x => x.Name == "Candidate")
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        var recruiterRoleId = await _context.Roles
            .AsNoTracking()
            .Where(x => x.Name == "Recruiter")
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        var totalCandidates = string.IsNullOrWhiteSpace(candidateRoleId)
            ? 0
            : await _context.UserRoles
                .AsNoTracking()
                .CountAsync(x => x.RoleId == candidateRoleId);

        var totalRecruiters = string.IsNullOrWhiteSpace(recruiterRoleId)
            ? 0
            : await _context.UserRoles
                .AsNoTracking()
                .CountAsync(x => x.RoleId == recruiterRoleId);

        var model = new HomeViewModel
        {
            LatestPositions = latestPositions,

            PopularPositions = popularPositions,

            TechnologyTags = technologyTags,

            Statistics = new DashboardStatisticsViewModel
            {
                NewCvsLast24Hours = newCvsLast24Hours,
                TotalPositions = totalPositions,
                TotalCandidates = totalCandidates,
                TotalRecruiters = totalRecruiters,
                TotalSubmittedCvs = totalSubmittedCvs
            },

            IsRecruiter = isRecruiter,

            IsAdministrator = isAdministrator
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
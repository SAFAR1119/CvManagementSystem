using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers;

[Authorize(Roles = "Candidate,Administrator")]
public class ProjectsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProjectsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? userId)
    {
        var targetUserId =
            await GetTargetUserIdAsync(userId);

        if (targetUserId == null)
        {
            return Forbid();
        }

        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        var projects =
            await _context.Projects
                .AsNoTracking()
                .Where(
                    x =>
                        x.CandidateProfileId ==
                        profile.Id)
                .Include(x => x.TechnologyTags)
                    .ThenInclude(x => x.TechnologyTag)
                .OrderByDescending(x => x.StartDate)
                .ThenBy(x => x.Name)
                .ToListAsync();

        var model =
            projects
                .Select(
                    x =>
                        new ProjectViewModel
                        {
                            Id = x.Id,
                            CandidateProfileId =
                                x.CandidateProfileId,
                            UserId =
                                targetUserId,
                            Name = x.Name,
                            StartDate =
                                x.StartDate,
                            EndDate =
                                x.EndDate,
                            DescriptionMarkdown =
                                x.DescriptionMarkdown,
                            TechnologyTags =
                                x.TechnologyTags
                                    .OrderBy(
                                        t =>
                                            t.TechnologyTag.Name)
                                    .Select(
                                        t =>
                                            t.TechnologyTag.Name)
                                    .ToList()
                        })
                .ToList();

        SetViewState(targetUserId);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        string? userId)
    {
        var targetUserId =
            await GetTargetUserIdAsync(userId);

        if (targetUserId == null)
        {
            return Forbid();
        }

        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        var model =
            new ProjectViewModel
            {
                CandidateProfileId =
                    profile.Id,

                UserId =
                    targetUserId,

                StartDate =
                    DateOnly.FromDateTime(
                        DateTime.Today)
            };

        await LoadAvailableTagsAsync(model);

        SetViewState(targetUserId);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProjectViewModel model)
    {
        var targetUserId =
            await GetTargetUserIdAsync(
                model.UserId);

        if (targetUserId == null)
        {
            return Forbid();
        }

        if (model.EndDate.HasValue &&
            model.EndDate.Value < model.StartDate)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                "End date cannot be earlier than the start date.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAvailableTagsAsync(model);
            SetViewState(targetUserId);

            return View(model);
        }

        var profile =
            await _context.CandidateProfiles
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        var tagNames =
            NormalizeTagNames(
                model.TechnologyTags);

        var tags =
            await ResolveTechnologyTagsAsync(
                tagNames);

        var project =
            new Project
            {
                CandidateProfileId =
                    profile.Id,

                Name =
                    model.Name.Trim(),

                StartDate =
                    model.StartDate,

                EndDate =
                    model.EndDate,

                DescriptionMarkdown =
                    model.DescriptionMarkdown.Trim(),

                UpdatedAt =
                    DateTime.UtcNow
            };

        foreach (var tag in tags)
        {
            project.TechnologyTags.Add(
                new ProjectTechnologyTag
                {
                    Project = project,
                    TechnologyTag = tag
                });
        }

        _context.Projects.Add(project);

        await _context.SaveChangesAsync();

        return RedirectToProjects(
            targetUserId);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        string? userId)
    {
        var targetUserId =
            await GetTargetUserIdAsync(
                userId);

        if (targetUserId == null)
        {
            return Forbid();
        }

        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        var project =
            await _context.Projects
                .AsNoTracking()
                .Where(
                    x =>
                        x.Id == id &&
                        x.CandidateProfileId ==
                        profile.Id)
                .Include(x => x.TechnologyTags)
                    .ThenInclude(
                        x => x.TechnologyTag)
                .FirstOrDefaultAsync();

        if (project == null)
        {
            return NotFound();
        }

        var model =
            new ProjectViewModel
            {
                Id =
                    project.Id,

                CandidateProfileId =
                    project.CandidateProfileId,

                UserId =
                    targetUserId,

                Name =
                    project.Name,

                StartDate =
                    project.StartDate,

                EndDate =
                    project.EndDate,

                DescriptionMarkdown =
                    project.DescriptionMarkdown,

                TechnologyTags =
                    project.TechnologyTags
                        .OrderBy(
                            x =>
                                x.TechnologyTag.Name)
                        .Select(
                            x =>
                                x.TechnologyTag.Name)
                        .ToList()
            };

        await LoadAvailableTagsAsync(model);

        SetViewState(targetUserId);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ProjectViewModel model)
    {
        var targetUserId =
            await GetTargetUserIdAsync(
                model.UserId);

        if (targetUserId == null)
        {
            return Forbid();
        }

        if (model.EndDate.HasValue &&
            model.EndDate.Value < model.StartDate)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                "End date cannot be earlier than the start date.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAvailableTagsAsync(model);
            SetViewState(targetUserId);

            return View(model);
        }

        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        var project =
            await _context.Projects
                .Include(x => x.TechnologyTags)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == model.Id &&
                        x.CandidateProfileId ==
                        profile.Id);

        if (project == null)
        {
            return NotFound();
        }

        project.Name =
            model.Name.Trim();

        project.StartDate =
            model.StartDate;

        project.EndDate =
            model.EndDate;

        project.DescriptionMarkdown =
            model.DescriptionMarkdown.Trim();

        project.UpdatedAt =
            DateTime.UtcNow;

        var currentLinks =
            project.TechnologyTags.ToList();

        _context.ProjectTechnologyTags
            .RemoveRange(currentLinks);

        project.TechnologyTags.Clear();

        var tagNames =
            NormalizeTagNames(
                model.TechnologyTags);

        var tags =
            await ResolveTechnologyTagsAsync(
                tagNames);

        foreach (var tag in tags)
        {
            project.TechnologyTags.Add(
                new ProjectTechnologyTag
                {
                    Project = project,
                    TechnologyTag = tag
                });
        }

        await _context.SaveChangesAsync();

        return RedirectToProjects(
            targetUserId);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        string? userId)
    {
        var targetUserId =
            await GetTargetUserIdAsync(
                userId);

        if (targetUserId == null)
        {
            return Forbid();
        }

        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        var project =
            await _context.Projects
                .AsNoTracking()
                .Where(
                    x =>
                        x.Id == id &&
                        x.CandidateProfileId ==
                        profile.Id)
                .Include(x => x.TechnologyTags)
                    .ThenInclude(
                        x => x.TechnologyTag)
                .FirstOrDefaultAsync();

        if (project == null)
        {
            return NotFound();
        }

        var model =
            new ProjectViewModel
            {
                Id =
                    project.Id,

                CandidateProfileId =
                    project.CandidateProfileId,

                UserId =
                    targetUserId,

                Name =
                    project.Name,

                StartDate =
                    project.StartDate,

                EndDate =
                    project.EndDate,

                DescriptionMarkdown =
                    project.DescriptionMarkdown,

                TechnologyTags =
                    project.TechnologyTags
                        .OrderBy(
                            x =>
                                x.TechnologyTag.Name)
                        .Select(
                            x =>
                                x.TechnologyTag.Name)
                        .ToList()
            };

        SetViewState(targetUserId);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int[] selectedIds,
        string? userId)
    {
        var targetUserId =
            await GetTargetUserIdAsync(
                userId);

        if (targetUserId == null)
        {
            return Forbid();
        }

        if (selectedIds.Length == 0)
        {
            TempData["Error"] =
                "Select at least one project.";

            return RedirectToProjects(
                targetUserId);
        }

        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        var projects =
            await _context.Projects
                .Where(
                    x =>
                        x.CandidateProfileId ==
                        profile.Id &&
                        selectedIds.Contains(
                            x.Id))
                .ToListAsync();

        _context.Projects.RemoveRange(
            projects);

        await _context.SaveChangesAsync();

        return RedirectToProjects(
            targetUserId);
    }

    [HttpGet]
    public async Task<IActionResult> SearchTags(
        string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Json(
                Array.Empty<string>());
        }

        var prefix =
            term.Trim();

        var tags =
            await _context.TechnologyTags
                .AsNoTracking()
                .Where(
                    x =>
                        EF.Functions.ILike(
                            x.Name,
                            prefix + "%"))
                .OrderBy(x => x.Name)
                .Take(10)
                .Select(x => x.Name)
                .ToListAsync();

        return Json(tags);
    }

    private async Task<string?>
    GetTargetUserIdAsync(
        string? requestedUserId)
{
    var currentUserId =
        _userManager.GetUserId(User);

    if (string.IsNullOrWhiteSpace(
            currentUserId))
    {
        return null;
    }

    // Candidates may only manage their own projects.
    // Ignore any posted userId and always use the
    // currently authenticated user's ID.
    if (!User.IsInRole(
            "Administrator"))
    {
        return currentUserId;
    }

    // Administrators may manage another candidate's projects.
    if (string.IsNullOrWhiteSpace(
            requestedUserId))
    {
        return currentUserId;
    }

    var exists =
        await _userManager.Users
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id ==
                    requestedUserId);

    return exists
        ? requestedUserId
        : null;
}

    private async Task
        LoadAvailableTagsAsync(
            ProjectViewModel model)
    {
        model.AvailableTechnologyTags =
            await _context.TechnologyTags
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Take(100)
                .Select(x => x.Name)
                .ToListAsync();
    }

    private async Task<List<TechnologyTag>>
        ResolveTechnologyTagsAsync(
            List<string> requestedNames)
    {
        if (requestedNames.Count == 0)
        {
            return new List<TechnologyTag>();
        }

        var normalized =
            requestedNames
                .Select(NormalizeTag)
                .Where(
                    x => x.Length > 0)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (normalized.Count == 0)
        {
            return new List<TechnologyTag>();
        }

        var existing =
            await _context.TechnologyTags
                .Where(
                    x =>
                        normalized.Contains(
                            x.Name))
                .ToListAsync();

        var existingNames =
            existing
                .Select(x => x.Name)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        var newTags =
            normalized
                .Where(
                    x =>
                        !existingNames.Contains(
                            x))
                .Select(
                    x =>
                        new TechnologyTag
                        {
                            Name = x
                        })
                .ToList();

        if (newTags.Count > 0)
        {
            await _context.TechnologyTags
                .AddRangeAsync(
                    newTags);
        }

        existing.AddRange(newTags);

        return existing
            .OrderBy(x => x.Name)
            .ToList();
    }

    private static List<string>
        NormalizeTagNames(
            IEnumerable<string> names)
    {
        return names
            .Select(NormalizeTag)
            .Where(
                x => x.Length > 0)
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeTag(
        string? value)
    {
        return value?.Trim()
            ?? string.Empty;
    }

    private void SetViewState(
        string targetUserId)
    {
        ViewBag.TargetUserId =
            targetUserId;

        ViewBag.IsAdministratorView =
            User.IsInRole("Administrator") &&
            targetUserId !=
                _userManager.GetUserId(User);
    }

    private IActionResult RedirectToProjects(
        string userId)
    {
        var currentUserId =
            _userManager.GetUserId(User);

        return RedirectToAction(
            nameof(Index),
            new
            {
                userId =
                    User.IsInRole("Administrator") &&
                    userId != currentUserId
                        ? userId
                        : null
            });
    }
}
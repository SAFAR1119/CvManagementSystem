using System.Security.Claims;
using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers;

[Authorize]
public class CvsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PositionAccessService _positionAccessService;
    private readonly CvGenerationService _cvGenerationService;

    public CvsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        PositionAccessService positionAccessService,
        CvGenerationService cvGenerationService)
    {
        _context = context;
        _userManager = userManager;
        _positionAccessService = positionAccessService;
        _cvGenerationService = cvGenerationService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? positionId,
        string? tag)
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var isAdmin = await _userManager.IsInRoleAsync(
            currentUser,
            "Administrator");

        var isRecruiter = await _userManager.IsInRoleAsync(
            currentUser,
            "Recruiter");

        var query = _context.Cvs
            .AsNoTracking()
            .Include(x => x.Position)
            .Include(x => x.CandidateProfile)
            .Include(x => x.CandidateProfile.User)
            .OrderByDescending(x => x.UpdatedAt)
            .AsQueryable();

        if (!isAdmin && !isRecruiter)
        {
            query = query.Where(x =>
                x.CandidateProfile.UserId == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var prefix = search.Trim();

            query = query.Where(x =>
                EF.Functions.ILike(x.Title, $"%{prefix}%") ||
                EF.Functions.ILike(x.Position.Title, $"%{prefix}%") ||
                EF.Functions.ILike(
                    x.CandidateProfile.FirstName,
                    $"%{prefix}%") ||
                EF.Functions.ILike(
                    x.CandidateProfile.LastName,
                    $"%{prefix}%"));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim();

            query = query.Where(x =>
            x.Projects.Any(cp =>
            cp.Project.TechnologyTags.Any(pt =>
                EF.Functions.ILike(
                    pt.TechnologyTag.Name,
                    normalizedTag))));
        }
        ViewBag.Tag = tag;

        if (positionId.HasValue)
        {
            query = query.Where(x =>
                x.PositionId == positionId.Value);
        }

        var cvs = await query
            .Select(x => new CvListViewModel
            {
                Id = x.Id,
                Title = x.Title,
                PositionId = x.PositionId,
                PositionTitle = x.Position.Title,
                CandidateProfileId = x.CandidateProfileId,
                CandidateName =
                    x.CandidateProfile.FirstName + " " +
                    x.CandidateProfile.LastName,
                IsPublished = x.IsPublished,
                UpdatedAt = x.UpdatedAt,
                LikeCount = _context.CvLikes.Count(l => l.CvId == x.Id)
            })
            .ToListAsync();

        var visibleCvs = new List<CvListViewModel>();

        if (isAdmin || isRecruiter)
        {
            visibleCvs.AddRange(cvs);
        }
        else
        {
            var accessiblePositionIds = new HashSet<int>();

            var candidateProfile = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(x => x.AttributeValues)
                .FirstOrDefaultAsync(x => x.UserId == currentUser.Id);

            if (candidateProfile != null)
            {
                var positions = await _context.Positions
                    .AsNoTracking()
                    .Include(x => x.AccessRules)
                    .ThenInclude(x => x.AttributeDefinition)
                    .ToListAsync();

                foreach (var position in positions)
                {
                    if (PositionAccessService.IsAuthorized(
                            position,
                            candidateProfile))
                    {
                        accessiblePositionIds.Add(position.Id);
                    }
                }
            }

            visibleCvs.AddRange(
                cvs.Where(x =>
                    accessiblePositionIds.Contains(x.PositionId)));
        }

        ViewBag.Search = search;
        ViewBag.PositionId = positionId;

        ViewBag.Positions = await _context.Positions
            .AsNoTracking()
            .OrderBy(x => x.Title)
            .Select(x => new
            {
                x.Id,
                x.Title
            })
            .ToListAsync();

        ViewBag.IsRecruiter = isRecruiter;
        ViewBag.IsAdministrator = isAdmin;

        return View(visibleCvs);
    }

    [Authorize(Roles = "Candidate,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int positionId)
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var targetUserId = currentUser.Id;

        if (User.IsInRole("Administrator"))
        {
            var candidateId = Request.Form["candidateUserId"]
                .ToString();

            if (!string.IsNullOrWhiteSpace(candidateId))
            {
                targetUserId = candidateId;
            }
        }

       var cv = await _cvGenerationService.GenerateAsync(
                 positionId,
                 targetUserId);

        if (cv == null)
        {
            TempData["ErrorMessage"] =
                "You are not currently authorized to create a CV for this position.";

            return RedirectToAction(
                "Details",
                "Positions",
                new { id = positionId });
        }

        return RedirectToAction(
            nameof(Details),
            new { id = cv.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var cv = await _context.Cvs
            .AsNoTracking()
            .Include(x => x.Position)
                .ThenInclude(x => x.Attributes)
                    .ThenInclude(x => x.AttributeDefinition)
                        .ThenInclude(x => x.Options)
            .Include(x => x.Position)
                .ThenInclude(x => x.AccessRules)
                    .ThenInclude(x => x.AttributeDefinition)
            .Include(x => x.CandidateProfile)
                .ThenInclude(x => x.AttributeValues)
                    .ThenInclude(x => x.AttributeDefinition)
                        .ThenInclude(x => x.Category)
            .Include(x => x.CandidateProfile)
                .ThenInclude(x => x.AttributeValues)
                    .ThenInclude(x => x.AttributeDefinition)
                        .ThenInclude(x => x.Options)
            .Include(x => x.Projects)
                .ThenInclude(x => x.Project)
                    .ThenInclude(x => x.TechnologyTags)
                        .ThenInclude(x => x.TechnologyTag)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cv == null)
        {
            return NotFound();
        }

        var isAdmin = await _userManager.IsInRoleAsync(
            currentUser,
            "Administrator");

        var isRecruiter = await _userManager.IsInRoleAsync(
            currentUser,
            "Recruiter");

        var isOwner =
            cv.CandidateProfile.UserId == currentUser.Id;

        if (!isAdmin && !isRecruiter && !isOwner)
        {
            return Forbid();
        }

        if (!isAdmin && !isRecruiter)
        {
            var canAccess = await _positionAccessService.CanAccessAsync(
                cv.PositionId,
                currentUser.Id);

            if (!canAccess)
            {
                return Forbid();
            }
        }

        var attributeValues =
            cv.CandidateProfile.AttributeValues
                .GroupBy(x => x.AttributeDefinitionId)
                .ToDictionary(
                    x => x.Key,
                    x => x.First());

        var attributes = cv.Position.Attributes
            .OrderBy(x => x.SortOrder)
            .Select(x =>
            {
                attributeValues.TryGetValue(
                    x.AttributeDefinitionId,
                    out var candidateValue);

                return new CvAttributeViewModel
                {
                    AttributeDefinitionId =
                        x.AttributeDefinitionId,

                    Name = x.AttributeDefinition.Name,

                    Category =
                        x.AttributeDefinition.Category?.Name ??
                        "General",

                    DataType =
                        x.AttributeDefinition.DataType,

                    Value = candidateValue?.Value,

                    IsRequired = x.IsRequired,

                    Version = candidateValue?.Version ??
                              Guid.Empty,

                    Options =
                        x.AttributeDefinition.Options
                            .OrderBy(o => o.SortOrder)
                            .Select(o => o.Value)
                            .ToList()
                };
            })
            .ToList();

        var projects = cv.Projects
            .OrderBy(x => x.SortOrder)
            .Select(x => new CvProjectViewModel
            {
                ProjectId = x.ProjectId,
                Name = x.Project.Name,
                Period = x.Project.EndDate.HasValue
                    ? $"{x.Project.StartDate:MMM yyyy} - {x.Project.EndDate.Value:MMM yyyy}"
                    : $"{x.Project.StartDate:MMM yyyy} - Present",
                DescriptionMarkdown =
                    x.Project.DescriptionMarkdown,
                TechnologyTags =
                    x.Project.TechnologyTags
                        .OrderBy(t => t.TechnologyTag.Name)
                        .Select(t => t.TechnologyTag.Name)
                        .ToList()
            })
            .ToList();

        var missingRequired =
            attributes.Count(x =>
                x.IsRequired &&
                string.IsNullOrWhiteSpace(x.Value));

        var model = new CvDetailsViewModel
        {
            Id = cv.Id,
            PositionId = cv.PositionId,
            PositionTitle = cv.Position.Title,
            CandidateFirstName =
                cv.CandidateProfile.FirstName,
            CandidateLastName =
                cv.CandidateProfile.LastName,
            Location =
                cv.CandidateProfile.Location,
            PhotoUrl =
                cv.CandidateProfile.PhotoUrl,
            ProfileVersion =
                cv.CandidateProfile.Version,
            IsPublished =
                cv.IsPublished,
            IsReadOnly =
                isRecruiter && !isAdmin,
            CanPublish =
                !isRecruiter &&
                missingRequired == 0 &&
                !string.IsNullOrWhiteSpace(
                    cv.CandidateProfile.FirstName) &&
                !string.IsNullOrWhiteSpace(
                    cv.CandidateProfile.LastName),
            MissingRequiredAttributes =
                missingRequired,
            Attributes = attributes,
            Projects = projects,
            LikeCount =
                await _context.CvLikes.CountAsync(
                    x => x.CvId == cv.Id)
        };

        return View(model);
    }

    [Authorize(Roles = "Candidate,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        CvDetailsViewModel model)
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var cv = await _context.Cvs
            .Include(x => x.Position)
                .ThenInclude(x => x.Attributes)
            .Include(x => x.CandidateProfile)
                .ThenInclude(x => x.AttributeValues)
            .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (cv == null)
        {
            return NotFound();
        }

        var isAdmin = await _userManager.IsInRoleAsync(
            currentUser,
            "Administrator");

        if (!isAdmin &&
            cv.CandidateProfile.UserId != currentUser.Id)
        {
            return Forbid();
        }

        if (cv.IsPublished && !isAdmin)
        {
            TempData["ErrorMessage"] =
                "A published CV cannot be changed.";

            return RedirectToAction(
                nameof(Details),
                new { id = cv.Id });
        }

        var canAccess = await _positionAccessService.CanAccessAsync(
            cv.PositionId,
            cv.CandidateProfile.UserId);

        if (!canAccess && !isAdmin)
        {
            return Forbid();
        }

        if (cv.CandidateProfile.Version != model.ProfileVersion)
        {
            TempData["ErrorMessage"] =
                "The profile was changed somewhere else. Reload the CV and try again.";

            return RedirectToAction(
                nameof(Details),
                new { id = cv.Id });
        }

        foreach (var item in model.Attributes)
        {
            var existing =
                cv.CandidateProfile.AttributeValues
                    .FirstOrDefault(x =>
                        x.AttributeDefinitionId ==
                        item.AttributeDefinitionId);

            if (existing == null)
            {
                cv.CandidateProfile.AttributeValues.Add(
                    new CandidateAttributeValue
                    {
                        AttributeDefinitionId =
                            item.AttributeDefinitionId,
                        Value = item.Value,
                        UpdatedAt = DateTime.UtcNow,
                        Version = Guid.NewGuid()
                    });

                continue;
            }

            if (existing.Version != item.Version &&
                item.Version != Guid.Empty)
            {
                TempData["ErrorMessage"] =
                    $"The attribute '{item.Name}' was changed somewhere else. Reload the CV.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = cv.Id });
            }

            existing.Value = item.Value;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = Guid.NewGuid();
        }

        cv.CandidateProfile.FirstName =
            model.CandidateFirstName.Trim();

        cv.CandidateProfile.LastName =
            model.CandidateLastName.Trim();

        cv.CandidateProfile.Location =
            string.IsNullOrWhiteSpace(model.Location)
                ? null
                : model.Location.Trim();

        cv.CandidateProfile.PhotoUrl =
            string.IsNullOrWhiteSpace(model.PhotoUrl)
                ? null
                : model.PhotoUrl.Trim();

        cv.CandidateProfile.UpdatedAt = DateTime.UtcNow;
        cv.CandidateProfile.Version = Guid.NewGuid();

        cv.UpdatedAt = DateTime.UtcNow;
        cv.Version = Guid.NewGuid();

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "CV saved successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = cv.Id });
    }

    [Authorize(Roles = "Candidate,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var cv = await _context.Cvs
            .Include(x => x.Position)
                .ThenInclude(x => x.Attributes)
            .Include(x => x.CandidateProfile)
                .ThenInclude(x => x.AttributeValues)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cv == null)
        {
            return NotFound();
        }

        var isAdmin = await _userManager.IsInRoleAsync(
            currentUser,
            "Administrator");

        if (!isAdmin &&
            cv.CandidateProfile.UserId != currentUser.Id)
        {
            return Forbid();
        }

        if (!isAdmin)
        {
            var canAccess = await _positionAccessService.CanAccessAsync(
                cv.PositionId,
                currentUser.Id);

            if (!canAccess)
            {
                return Forbid();
            }
        }

        var values = cv.CandidateProfile.AttributeValues
            .GroupBy(x => x.AttributeDefinitionId)
            .ToDictionary(
                x => x.Key,
                x => x.First().Value);

        var missingRequired = cv.Position.Attributes
            .Where(x => x.IsRequired)
            .Any(x =>
                !values.TryGetValue(
                    x.AttributeDefinitionId,
                    out var value) ||
                string.IsNullOrWhiteSpace(value));

        if (missingRequired ||
            string.IsNullOrWhiteSpace(
                cv.CandidateProfile.FirstName) ||
            string.IsNullOrWhiteSpace(
                cv.CandidateProfile.LastName))
        {
            TempData["ErrorMessage"] =
                "Complete all required CV fields before publishing.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        cv.IsPublished = true;
        cv.UpdatedAt = DateTime.UtcNow;
        cv.Version = Guid.NewGuid();

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "CV published successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }
}
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

        // Administrators have unrestricted access, including drafts. A user
        // may hold both roles, so the administrator check must come first.
        if (!isAdmin && isRecruiter)
        {
             query = query.Where(x =>
                 x.IsPublished);
        }
        else if (!isAdmin)
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
            .Include(x => x.CandidateProfile)
                .ThenInclude(x => x.EducationEntries)
            .Include(x => x.CandidateProfile)
                .ThenInclude(x => x.WorkExperiences)
            .Include(x => x.CandidateProfile)
                .ThenInclude(x => x.Projects)
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

        var isLikedByCurrentUser = false;

if (isRecruiter || isAdmin)
{
    isLikedByCurrentUser =
        await _context.CvLikes.AnyAsync(
            x =>
                x.CvId == cv.Id &&
                x.RecruiterId == currentUser.Id);
}

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

        var requiredAttributeIds = cv.Position.Attributes
            .Where(x => x.IsRequired)
            .Select(x => x.AttributeDefinitionId)
            .ToHashSet();

        // A CV is a complete candidate profile.  Position attributes remain
        // marked as required for publishing, but optional profile attributes
        // (for example languages, certifications, and custom fields) must not
        // disappear merely because the position did not explicitly request them.
        var attributes = cv.CandidateProfile.AttributeValues
            .OrderBy(x => x.AttributeDefinition.Category!.Name)
            .ThenBy(x => x.AttributeDefinition.Name)
            .Select(x =>
            {
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

                    Value = x.Value,

                    IsRequired = requiredAttributeIds.Contains(
                        x.AttributeDefinitionId),

                    Version = x.Version,

                    Options =
                        x.AttributeDefinition.Options
                            .OrderBy(o => o.SortOrder)
                            .Select(o => o.Value)
                            .ToList()
                };
            })
            .ToList();

        // Projects belong to the profile, so a project added after a CV was
        // generated is immediately visible on every CV and to recruiters.
        var projects = cv.CandidateProfile.Projects
            .OrderByDescending(x => x.EndDate ?? DateOnly.MaxValue)
            .ThenByDescending(x => x.StartDate)
            .Select(x => new CvProjectViewModel
            {
                ProjectId = x.Id,
                Name = x.Name,
                Period = x.EndDate.HasValue
                    ? $"{x.StartDate:MMM yyyy} - {x.EndDate.Value:MMM yyyy}"
                    : $"{x.StartDate:MMM yyyy} - Present",
                DescriptionMarkdown =
                    x.DescriptionMarkdown,
                TechnologyTags =
                    x.TechnologyTags
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
            ProfessionalTitle = cv.CandidateProfile.ProfessionalTitle,
            ProfessionalSummary = cv.CandidateProfile.ProfessionalSummary,
            PhoneNumber = cv.CandidateProfile.PhoneNumber,
            Email = cv.CandidateProfile.Email,
            LinkedInUrl = cv.CandidateProfile.LinkedInUrl,
            GitHubUrl = cv.CandidateProfile.GitHubUrl,
            PortfolioUrl = cv.CandidateProfile.PortfolioUrl,
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
            EducationEntries = cv.CandidateProfile.EducationEntries
                .OrderByDescending(x => x.EndDate ?? DateOnly.MaxValue)
                .ThenByDescending(x => x.StartDate)
                .Select(x => new EducationViewModel { Id = x.Id, Degree = x.Degree, Institution = x.Institution, StartDate = x.StartDate, EndDate = x.EndDate, Description = x.Description, SortOrder = x.SortOrder })
                .ToList(),
            WorkExperiences = cv.CandidateProfile.WorkExperiences
                .OrderByDescending(x => x.EndDate ?? DateOnly.MaxValue)
                .ThenByDescending(x => x.StartDate)
                .Select(x => new WorkExperienceViewModel { Id = x.Id, CompanyName = x.CompanyName, JobTitle = x.JobTitle, StartDate = x.StartDate, EndDate = x.EndDate, Description = x.Description, Technologies = x.Technologies, SortOrder = x.SortOrder })
                .ToList(),
            LikeCount =
                await _context.CvLikes.CountAsync(
                    x => x.CvId == cv.Id),
            IsLikedByCurrentUser = isLikedByCurrentUser
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
            .ThenInclude(x => x.AttributeDefinition)
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

        cv.CandidateProfile.ProfessionalTitle = Normalize(model.ProfessionalTitle);
        cv.CandidateProfile.ProfessionalSummary = Normalize(model.ProfessionalSummary);
        cv.CandidateProfile.PhoneNumber = Normalize(model.PhoneNumber);
        cv.CandidateProfile.Email = Normalize(model.Email);
        cv.CandidateProfile.LinkedInUrl = Normalize(model.LinkedInUrl);
        cv.CandidateProfile.GitHubUrl = Normalize(model.GitHubUrl);
        cv.CandidateProfile.PortfolioUrl = Normalize(model.PortfolioUrl);

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
                    .ThenInclude(x => x.AttributeDefinition)
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
            .Where(x =>
                !values.TryGetValue(
                    x.AttributeDefinitionId,
                    out var value) ||
                string.IsNullOrWhiteSpace(value))
            .Select(x => x.AttributeDefinition?.Name ?? "Required attribute")
            .ToList();

        if (missingRequired.Count > 0 ||
            string.IsNullOrWhiteSpace(
                cv.CandidateProfile.FirstName) ||
            string.IsNullOrWhiteSpace(
                cv.CandidateProfile.LastName))
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(cv.CandidateProfile.FirstName))
            {
                missingFields.Add("First name");
            }

            if (string.IsNullOrWhiteSpace(cv.CandidateProfile.LastName))
            {
                missingFields.Add("Last name");
            }

            missingFields.AddRange(missingRequired);

            TempData["ErrorMessage"] =
                "Complete the required fields before publishing: " +
                string.Join(", ", missingFields) + ".";

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


    [Authorize(Roles = "Recruiter")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleLike(int id)
{
    var currentUser = await _userManager.GetUserAsync(User);

    if (currentUser == null)
    {
        return Challenge();
    }

    var cv = await _context.Cvs
        .AsNoTracking()
        .Select(x => new
        {
            x.Id,
            x.IsPublished
        })
        .FirstOrDefaultAsync(x => x.Id == id);

    if (cv == null)
    {
        return NotFound();
    }

    if (!cv.IsPublished)
    {
        return BadRequest(new
        {
            message = "Only published CVs can be liked."
        });
    }

    var existingLike = await _context.CvLikes
        .FirstOrDefaultAsync(
            x =>
                x.CvId == id &&
                x.RecruiterId == currentUser.Id);

    if (existingLike == null)
    {
        _context.CvLikes.Add(
            new CvLike
            {
                CvId = id,
                RecruiterId = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            });
    }
    else
    {
        _context.CvLikes.Remove(existingLike);
    }

    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        /*
         * The unique (CvId, RecruiterId) index protects
         * against duplicate likes if two requests arrive
         * at approximately the same time.
         */
    }

    var liked = await _context.CvLikes
        .AsNoTracking()
        .AnyAsync(
            x =>
                x.CvId == id &&
                x.RecruiterId == currentUser.Id);

    var likeCount = await _context.CvLikes
        .AsNoTracking()
        .CountAsync(x => x.CvId == id);

    return Json(new
    {
        liked,
        likeCount
    });
  }

  [Authorize(Roles = "Candidate,Administrator")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteSelected(
    int[] selectedIds)
{
    var currentUser =
        await _userManager.GetUserAsync(User);

    if (currentUser == null)
    {
        return Challenge();
    }

    var ids = selectedIds?
        .Where(x => x > 0)
        .Distinct()
        .ToList()
        ?? new List<int>();

    if (ids.Count == 0)
    {
        TempData["ErrorMessage"] =
            "Select at least one CV.";

        return RedirectToAction(
            "Index",
            "Profile");
    }

    var isAdministrator =
        await _userManager.IsInRoleAsync(
            currentUser,
            "Administrator");

    var query = _context.Cvs
        .Where(x => ids.Contains(x.Id));

    if (!isAdministrator)
    {
        query = query.Where(x =>
            x.CandidateProfile.UserId ==
            currentUser.Id);
    }

    var cvs = await query.ToListAsync();

    if (cvs.Count == 0)
    {
        TempData["ErrorMessage"] =
            "No matching CVs were found.";

        return RedirectToAction(
            "Index",
            "Profile");
    }

    _context.Cvs.RemoveRange(cvs);

    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] =
        $"{cvs.Count} CV(s) deleted successfully.";

    return RedirectToAction(
        "Index",
        "Profile");
}

private static string? Normalize(string? value)
{
    return string.IsNullOrWhiteSpace(value)
        ? null
        : value.Trim();
}
}

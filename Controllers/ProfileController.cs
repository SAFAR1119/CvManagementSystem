using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers;

[Authorize(Roles = "Candidate,Administrator")]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }


    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? userId,
        string? attributeSearch,
        int? attributeCategoryId)
    {
        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }


        var isAdministrator =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");


        var targetUserId =
            isAdministrator &&
            !string.IsNullOrWhiteSpace(userId)
                ? userId
                : currentUser.Id;


        var profile =
            await LoadProfileAsync(
                targetUserId);


        if (profile == null)
        {
            profile =
                new CandidateProfile
                {
                    UserId =
                        targetUserId,

                    FirstName =
                        string.Empty,

                    LastName =
                        string.Empty,

                    Location =
                        null,

                    PhotoUrl =
                        null,

                    UpdatedAt =
                        DateTime.UtcNow,

                    Version =
                        Guid.NewGuid()
                };

            _context.CandidateProfiles.Add(
                profile);

            await _context.SaveChangesAsync();

            profile =
                await LoadProfileAsync(
                    targetUserId);
        }


        if (profile == null)
        {
            return NotFound();
        }


        var model =
            await BuildViewModelAsync(
                profile,
                attributeSearch,
                attributeCategoryId,
                isAdministrator &&
                profile.UserId != currentUser.Id);


        return View(model);
    }


    // =========================================================
    // SAVE
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        CandidateProfileViewModel model)
    {
        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }


        var isAdministrator =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");


        var targetUserId =
            isAdministrator &&
            !string.IsNullOrWhiteSpace(model.UserId)
                ? model.UserId
                : currentUser.Id;


        var profile =
            await _context.CandidateProfiles
                .Include(x => x.AttributeValues)
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId ==
                        targetUserId);


        if (profile == null)
        {
            return NotFound();
        }


        // ------------------------------------------------------
        // Profile optimistic locking
        // ------------------------------------------------------

        if (profile.Version != model.Version)
        {
            TempData["ErrorMessage"] =
                "The profile was changed by another user. Reload the page and try again.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    userId = targetUserId
                });
        }


        // ------------------------------------------------------
        // Built-in fields
        // ------------------------------------------------------

        profile.FirstName =
            (model.FirstName ?? string.Empty)
                .Trim();

        profile.LastName =
            (model.LastName ?? string.Empty)
                .Trim();

        profile.Location =
            string.IsNullOrWhiteSpace(
                model.Location)
                ? null
                : model.Location.Trim();

        profile.PhotoUrl =
            string.IsNullOrWhiteSpace(
                model.PhotoUrl)
                ? null
                : model.PhotoUrl.Trim();


        // ------------------------------------------------------
        // Attribute optimistic locking
        // ------------------------------------------------------

        foreach (var attribute
                 in model.Attributes)
        {
            var existing =
                profile.AttributeValues
                    .FirstOrDefault(
                        x =>
                            x.AttributeDefinitionId ==
                            attribute.AttributeDefinitionId);


            if (existing == null)
            {
                profile.AttributeValues.Add(
                    new CandidateAttributeValue
                    {
                        AttributeDefinitionId =
                            attribute.AttributeDefinitionId,

                        Value =
                            attribute.Value,

                        UpdatedAt =
                            DateTime.UtcNow,

                        Version =
                            Guid.NewGuid()
                    });

                continue;
            }


            if (attribute.Version != Guid.Empty &&
                existing.Version !=
                attribute.Version)
            {
                TempData["ErrorMessage"] =
                    $"The attribute '{attribute.Name}' was changed elsewhere. Reload the page and try again.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        userId =
                            targetUserId
                    });
            }


            existing.Value =
                attribute.Value;

            existing.UpdatedAt =
                DateTime.UtcNow;

            existing.Version =
                Guid.NewGuid();
        }


        profile.UpdatedAt =
            DateTime.UtcNow;

        profile.Version =
            Guid.NewGuid();


        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["ErrorMessage"] =
                "The profile was changed by another user. Reload the page and try again.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    userId =
                        targetUserId
                });
        }


        TempData["SuccessMessage"] =
            "Profile saved successfully.";


        return RedirectToAction(
            nameof(Index),
            new
            {
                userId =
                    isAdministrator &&
                    targetUserId != currentUser.Id
                        ? targetUserId
                        : null
            });
    }


    // =========================================================
    // AUTO SAVE
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoSave(
        ProfileAutoSaveViewModel model)
    {
        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Unauthorized();
        }


        var isAdministrator =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");


        var targetUserId =
            isAdministrator &&
            !string.IsNullOrWhiteSpace(
                model.UserId)
                ? model.UserId
                : currentUser.Id;


        var profile =
            await _context.CandidateProfiles
                .Include(x => x.AttributeValues)
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId ==
                        targetUserId);


        if (profile == null)
        {
            return NotFound(
                new
                {
                    success = false,
                    message =
                        "Candidate profile was not found."
                });
        }


        // ------------------------------------------------------
        // Profile version
        // ------------------------------------------------------

        if (profile.Version !=
            model.Version)
        {
            return Conflict(
                new
                {
                    success = false,
                    conflict = true,
                    message =
                        "This profile was changed somewhere else. Reload the page before continuing."
                });
        }


        profile.FirstName =
            (model.FirstName ?? string.Empty)
                .Trim();

        profile.LastName =
            (model.LastName ?? string.Empty)
                .Trim();

        profile.Location =
            string.IsNullOrWhiteSpace(
                model.Location)
                ? null
                : model.Location.Trim();

        profile.PhotoUrl =
            string.IsNullOrWhiteSpace(
                model.PhotoUrl)
                ? null
                : model.PhotoUrl.Trim();


        var submittedAttributeIds =
            model.Attributes
                .Where(
                    x =>
                        x.AttributeDefinitionId > 0)
                .Select(
                    x =>
                        x.AttributeDefinitionId)
                .Distinct()
                .ToList();


        var validAttributeIds =
            submittedAttributeIds.Count == 0
                ? new HashSet<int>()
                : (
                    await _context.AttributeDefinitions
                        .AsNoTracking()
                        .Where(
                            x =>
                                submittedAttributeIds
                                    .Contains(x.Id))
                        .Select(x => x.Id)
                        .ToListAsync()
                  )
                  .ToHashSet();


        if (validAttributeIds.Count !=
            submittedAttributeIds.Count)
        {
            return BadRequest(
                new
                {
                    success = false,
                    message =
                        "One or more selected attributes no longer exist."
                });
        }


        foreach (var attribute
                 in model.Attributes)
        {
            if (!validAttributeIds.Contains(
                    attribute.AttributeDefinitionId))
            {
                continue;
            }


            var existing =
                profile.AttributeValues
                    .FirstOrDefault(
                        x =>
                            x.AttributeDefinitionId ==
                            attribute.AttributeDefinitionId);


            if (existing == null)
            {
                profile.AttributeValues.Add(
                    new CandidateAttributeValue
                    {
                        AttributeDefinitionId =
                            attribute.AttributeDefinitionId,

                        Value =
                            attribute.Value,

                        UpdatedAt =
                            DateTime.UtcNow,

                        Version =
                            Guid.NewGuid()
                    });

                continue;
            }


            if (attribute.Version !=
                    Guid.Empty &&
                existing.Version !=
                    attribute.Version)
            {
                return Conflict(
                    new
                    {
                        success = false,
                        conflict = true,

                        attributeDefinitionId =
                            attribute.AttributeDefinitionId,

                        message =
                            "One of the profile attributes was changed elsewhere. Reload the page before continuing."
                    });
            }


            existing.Value =
                attribute.Value;

            existing.UpdatedAt =
                DateTime.UtcNow;

            existing.Version =
                Guid.NewGuid();
        }


        profile.UpdatedAt =
            DateTime.UtcNow;

        profile.Version =
            Guid.NewGuid();


        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(
                new
                {
                    success = false,
                    conflict = true,
                    message =
                        "The profile was changed by another user. Reload the page before continuing."
                });
        }


        var attributeVersions =
            profile.AttributeValues
                .Where(
                    x =>
                        validAttributeIds.Contains(
                            x.AttributeDefinitionId))
                .ToDictionary(
                    x =>
                        x.AttributeDefinitionId,
                    x =>
                        x.Version);


        return Json(
            new
            {
                success = true,

                profileVersion =
                    profile.Version,

                attributeVersions
            });
    }


    // =========================================================
    // ADD ATTRIBUTE
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAttribute(
        int attributeDefinitionId,
        string? userId)
    {
        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }


        var isAdministrator =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");


        var targetUserId =
            isAdministrator &&
            !string.IsNullOrWhiteSpace(userId)
                ? userId
                : currentUser.Id;


        var profile =
            await _context.CandidateProfiles
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId ==
                        targetUserId);


        if (profile == null)
        {
            return NotFound();
        }


        var definition =
            await _context.AttributeDefinitions
                .FirstOrDefaultAsync(
                    x =>
                        x.Id ==
                        attributeDefinitionId);


        if (definition == null)
        {
            return NotFound();
        }


        var exists =
            await _context.CandidateAttributeValues
                .AnyAsync(
                    x =>
                        x.CandidateProfileId ==
                        profile.Id &&
                        x.AttributeDefinitionId ==
                        attributeDefinitionId);


        if (!exists)
        {
            _context.CandidateAttributeValues.Add(
                new CandidateAttributeValue
                {
                    CandidateProfileId =
                        profile.Id,

                    AttributeDefinitionId =
                        attributeDefinitionId,

                    Value =
                        null,

                    UpdatedAt =
                        DateTime.UtcNow,

                    Version =
                        Guid.NewGuid()
                });

            definition.LastUsedAt =
                DateTime.UtcNow;

            definition.UsageCount++;

            await _context.SaveChangesAsync();
        }


        return RedirectToAction(
            nameof(Index),
            new
            {
                userId =
                    isAdministrator &&
                    targetUserId != currentUser.Id
                        ? targetUserId
                        : null
            });
    }


    // =========================================================
    // REMOVE ATTRIBUTE
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAttribute(
        int[] attributeIds,
        string? userId)
    {
        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Unauthorized();
        }


        var isAdministrator =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");


        var targetUserId =
            isAdministrator &&
            !string.IsNullOrWhiteSpace(userId)
                ? userId
                : currentUser.Id;


        var ids =
            attributeIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList()
            ?? new List<int>();


        if (ids.Count > 0)
        {
            var profile =
                await _context.CandidateProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.UserId ==
                            targetUserId);


            if (profile != null)
            {
                var values =
                    await _context.CandidateAttributeValues
                        .Where(
                            x =>
                                x.CandidateProfileId ==
                                profile.Id &&
                                ids.Contains(
                                    x.AttributeDefinitionId))
                        .ToListAsync();


                if (values.Count > 0)
                {
                    _context.CandidateAttributeValues
                        .RemoveRange(values);

                    await _context.SaveChangesAsync();
                }
            }
        }


        return RedirectToAction(
            nameof(Index),
            new
            {
                userId =
                    isAdministrator &&
                    targetUserId != currentUser.Id
                        ? targetUserId
                        : null
            });
    }


    // =========================================================
    // PUBLIC PROFILE
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpGet]
    public async Task<IActionResult> Public(
        string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest();
        }


        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .Include(x => x.Projects)
                    .ThenInclude(x =>
                        x.TechnologyTags)
                        .ThenInclude(x =>
                            x.TechnologyTag)
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId ==
                        userId);


        if (profile == null)
        {
            var user =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            userId);


            if (user == null)
            {
                return NotFound();
            }


            ViewBag.DisplayName =
                user.UserName ??
                user.Email ??
                "User";


            return View(
                "Public",
                null);
        }


        return View(
            "Public",
            profile);
    }


    // =========================================================
    // PRIVATE: LOAD PROFILE
    // =========================================================

    private async Task<CandidateProfile?> LoadProfileAsync(
        string userId)
    {
        return await _context.CandidateProfiles
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x =>
                x.AttributeValues)
                .ThenInclude(x =>
                    x.AttributeDefinition)
                    .ThenInclude(x =>
                        x.Category)
            .Include(x =>
                x.AttributeValues)
                .ThenInclude(x =>
                    x.AttributeDefinition)
                    .ThenInclude(x =>
                        x.Options)
            .Include(x =>
                x.Projects)
                .ThenInclude(x =>
                    x.TechnologyTags)
                    .ThenInclude(x =>
                        x.TechnologyTag)
            .Include(x =>
                x.Cvs)
                .ThenInclude(x =>
                    x.Position)
            .FirstOrDefaultAsync(
                x =>
                    x.UserId ==
                    userId);
    }


    // =========================================================
    // PRIVATE: BUILD VIEW MODEL
    // =========================================================

    private async Task<CandidateProfileViewModel>
        BuildViewModelAsync(
            CandidateProfile profile,
            string? attributeSearch,
            int? attributeCategoryId,
            bool isAdministratorView)
    {
        var candidateAttributeDefinitionIds =
            profile.AttributeValues
                .Select(
                    x =>
                        x.AttributeDefinitionId)
                .ToHashSet();


        var availableQuery =
            _context.AttributeDefinitions
                .AsNoTracking()
                .Include(x =>
                    x.Category)
                .Where(
                    x =>
                        !candidateAttributeDefinitionIds
                            .Contains(x.Id));


        if (!string.IsNullOrWhiteSpace(
                attributeSearch))
        {
            var prefix =
                attributeSearch.Trim();

            availableQuery =
                availableQuery.Where(
                    x =>
                        EF.Functions.ILike(
                            x.Name,
                            prefix + "%"));
        }


        if (attributeCategoryId.HasValue)
        {
            availableQuery =
                availableQuery.Where(
                    x =>
                        x.CategoryId ==
                        attributeCategoryId.Value);
        }


        var availableAttributes =
            await availableQuery
                .OrderByDescending(
                    x =>
                        x.LastUsedAt)
                .ThenBy(
                    x =>
                        x.Name)
                .Take(50)
                .Select(
                    x =>
                        new AvailableProfileAttributeViewModel
                        {
                            Id =
                                x.Id,

                            Name =
                                x.Name,

                            Category =
                                x.Category.Name,

                            DataType =
                                x.DataType,

                            LastUsedAt =
                                x.LastUsedAt
                        })
                .ToListAsync();


        var attributeCategories =
            await _context.AttributeCategories
                .AsNoTracking()
                .OrderBy(
                    x =>
                        x.Name)
                .Select(
                    x =>
                        new
                        {
                            x.Id,
                            x.Name
                        })
                .ToListAsync();


        ViewBag.AttributeCategories =
            attributeCategories;


        // ------------------------------------------------------
        // Accessible positions
        // ------------------------------------------------------

        var positions =
            await _context.Positions
                .AsNoTracking()
                .Include(x =>
                    x.AccessRules)
                    .ThenInclude(x =>
                        x.AttributeDefinition)
                .ToListAsync();


        var accessiblePositionIds =
            positions
                .Where(
                    x =>
                        PositionAccessService
                            .IsAuthorized(
                                x,
                                profile))
                .Select(
                    x =>
                        x.Id)
                .ToHashSet();


        // ------------------------------------------------------
        // Visible CVs
        //
        // Existing CVs are retained in DB when access is lost,
        // but hidden from the Candidate UI.
        // ------------------------------------------------------

        var visibleCvs =
            profile.Cvs
                .Where(
                    x =>
                        accessiblePositionIds.Contains(
                            x.PositionId))
                .OrderByDescending(
                    x =>
                        x.UpdatedAt)
                .ToList();


        var model =
            new CandidateProfileViewModel
            {
                Id =
                    profile.Id,

                UserId =
                    profile.UserId,

                FirstName =
                    profile.FirstName,

                LastName =
                    profile.LastName,

                Location =
                    profile.Location,

                PhotoUrl =
                    profile.PhotoUrl,

                Version =
                    profile.Version,

                AttributeSearch =
                    attributeSearch,

                AttributeCategoryId =
                    attributeCategoryId,

                IsAdministratorView =
                    isAdministratorView,

                AvailableAttributes =
                    availableAttributes,

                Attributes =
                    profile.AttributeValues
                        .OrderBy(
                            x =>
                                x.AttributeDefinition.Category.Name)
                        .ThenBy(
                            x =>
                                x.AttributeDefinition.Name)
                        .Select(
                            x =>
                                new CandidateAttributeViewModel
                                {
                                    AttributeDefinitionId =
                                        x.AttributeDefinitionId,

                                    Name =
                                        x.AttributeDefinition.Name,

                                    Category =
                                        x.AttributeDefinition
                                            .Category.Name,

                                    DataType =
                                        x.AttributeDefinition
                                            .DataType,

                                    Value =
                                        x.Value,

                                    Version =
                                        x.Version,

                                    Options =
                                        x.AttributeDefinition.Options
                                            .OrderBy(o => o.SortOrder)
                                            .Select(o =>
                                                new CandidateAttributeOptionViewModel
                                                {
                                                    Id = o.Id,
                                                    Value = o.Value
                                                })
                                            .ToList(),
                                })
                        .ToList(),

                Projects =
                    profile.Projects
                        .OrderByDescending(
                            x =>
                                x.StartDate)
                        .Select(
                            x =>
                                new CandidateProjectViewModel
                                {
                                    Id =
                                        x.Id,

                                    Name =
                                        x.Name,

                                    Period =
                                        x.EndDate.HasValue
                                            ? $"{x.StartDate:MMM yyyy} - {x.EndDate.Value:MMM yyyy}"
                                            : $"{x.StartDate:MMM yyyy} - Present",

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
                        .ToList(),

                Cvs =
                    visibleCvs
                        .Select(
                            x =>
                                new CandidateCvSummaryViewModel
                                {
                                    Id =
                                        x.Id,

                                    Title =
                                        x.Title,

                                    PositionTitle =
                                        x.Position.Title,

                                    IsPublished =
                                        x.IsPublished,

                                    UpdatedAt =
                                        x.UpdatedAt
                                })
                        .ToList()
            };


        return model;
    }
}

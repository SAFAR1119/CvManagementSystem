using CvManagementSystem.Data;
using CvManagementSystem.Models;
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

    [HttpGet]
    public async Task<IActionResult> Index(
        string? userId,
        string? attributeSearch,
        int? attributeCategoryId)
    {
        var targetUserId =
            await GetTargetUserIdAsync(userId);

        if (targetUserId == null)
        {
            return Forbid();
        }

        var profile =
            await LoadProfileAsync(targetUserId);

        if (profile == null)
        {
            profile = new CandidateProfile
            {
                UserId = targetUserId,
                FirstName = "New",
                LastName = "Candidate",
                Location = null,
                PhotoUrl = null,
                UpdatedAt = DateTime.UtcNow,
                Version = Guid.NewGuid()
            };

            _context.CandidateProfiles.Add(profile);

            await _context.SaveChangesAsync();

            profile =
                await LoadProfileAsync(targetUserId);

            if (profile == null)
            {
                return Problem(
                    "The candidate profile could not be created.");
            }
        }

        var model =
            await BuildViewModelAsync(
                profile,
                attributeSearch,
                attributeCategoryId);

        model.IsAdministratorView =
            User.IsInRole("Administrator") &&
            !string.Equals(
                targetUserId,
                _userManager.GetUserId(User),
                StringComparison.Ordinal);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        CandidateProfileViewModel model,
        string? userId)
    {
        var targetUserId =
            await GetTargetUserIdAsync(userId);

        if (targetUserId == null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var existingProfile =
                await LoadProfileAsync(targetUserId);

            if (existingProfile != null)
            {
                await RebuildListsAsync(
                    model,
                    existingProfile);
            }

            return View("Index", model);
        }

        var profile =
            await _context.CandidateProfiles
                .Include(x => x.AttributeValues)
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        if (profile.Version != model.Version)
        {
            ModelState.AddModelError(
                string.Empty,
                "This profile was changed by another user. " +
                "Reload the page before saving again.");

            await RebuildListsAsync(
                model,
                profile);

            return View("Index", model);
        }

        var submittedAttributeIds =
            model.Attributes
                .Select(x => x.AttributeDefinitionId)
                .ToHashSet();

        foreach (var attributeValue in profile.AttributeValues)
        {
            if (!submittedAttributeIds.Contains(
                    attributeValue.AttributeDefinitionId))
            {
                continue;
            }

            var submitted =
                model.Attributes.FirstOrDefault(
                    x => x.AttributeDefinitionId ==
                         attributeValue.AttributeDefinitionId);

            if (submitted == null)
            {
                continue;
            }

            if (attributeValue.Version != submitted.Version)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"The attribute '{submitted.Name}' was changed " +
                    "by another user. Reload the page and try again.");

                await RebuildListsAsync(
                    model,
                    profile);

                return View("Index", model);
            }
        }

        profile.FirstName =
            model.FirstName.Trim();

        profile.LastName =
            model.LastName.Trim();

        profile.Location =
            string.IsNullOrWhiteSpace(model.Location)
                ? null
                : model.Location.Trim();

        profile.PhotoUrl =
            string.IsNullOrWhiteSpace(model.PhotoUrl)
                ? null
                : model.PhotoUrl.Trim();

        profile.UpdatedAt =
            DateTime.UtcNow;

        profile.Version =
            Guid.NewGuid();

        foreach (var attributeValue in profile.AttributeValues)
        {
            var submitted =
                model.Attributes.FirstOrDefault(
                    x => x.AttributeDefinitionId ==
                         attributeValue.AttributeDefinitionId);

            if (submitted == null)
            {
                continue;
            }

            attributeValue.Value =
                string.IsNullOrWhiteSpace(submitted.Value)
                    ? null
                    : submitted.Value.Trim();

            attributeValue.UpdatedAt =
                DateTime.UtcNow;

            attributeValue.Version =
                Guid.NewGuid();
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The profile was changed by another user. " +
                "Reload the page and try again.");

            var latest =
                await LoadProfileAsync(targetUserId);

            if (latest != null)
            {
                await RebuildListsAsync(
                    model,
                    latest);
            }

            return View("Index", model);
        }

        TempData["Success"] =
            "Profile saved successfully.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                userId =
                    User.IsInRole("Administrator")
                        ? targetUserId
                        : null
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAttribute(
        int attributeDefinitionId,
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
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        var alreadySelected =
            await _context.CandidateAttributeValues
                .AnyAsync(
                    x =>
                        x.CandidateProfileId == profile.Id &&
                        x.AttributeDefinitionId ==
                        attributeDefinitionId);

        if (alreadySelected)
        {
            TempData["Error"] =
                "This attribute is already in the profile.";

            return RedirectToProfile(targetUserId);
        }

        var definition =
            await _context.AttributeDefinitions
                .FirstOrDefaultAsync(
                    x => x.Id == attributeDefinitionId);

        if (definition == null)
        {
            return NotFound();
        }

        var value =
            new CandidateAttributeValue
            {
                CandidateProfileId = profile.Id,
                AttributeDefinitionId =
                    definition.Id,
                Value = null,
                UpdatedAt = DateTime.UtcNow,
                Version = Guid.NewGuid()
            };

        _context.CandidateAttributeValues.Add(value);

        definition.LastUsedAt =
            DateTime.UtcNow;

        definition.UsageCount++;

        await _context.SaveChangesAsync();

        return RedirectToProfile(targetUserId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAttribute(
        int attributeId,
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
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        if (profile == null)
        {
            return NotFound();
        }

        var value =
            await _context.CandidateAttributeValues
                .FirstOrDefaultAsync(
                    x =>
                        x.CandidateProfileId == profile.Id &&
                        x.AttributeDefinitionId ==
                        attributeId);

        if (value == null)
        {
            return RedirectToProfile(targetUserId);
        }

        _context.CandidateAttributeValues.Remove(
            value);

        await _context.SaveChangesAsync();

        return RedirectToProfile(targetUserId);
    }

    private async Task<string?> GetTargetUserIdAsync(
        string? requestedUserId)
    {
        var currentUserId =
            _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(requestedUserId))
        {
            return currentUserId;
        }

        if (!User.IsInRole("Administrator"))
        {
            return null;
        }

        var exists =
            await _userManager.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == requestedUserId);

        return exists
            ? requestedUserId
            : null;
    }

    private async Task<CandidateProfile?> LoadProfileAsync(
        string userId)
    {
        return await _context.CandidateProfiles
            .AsSplitQuery()
            .Include(x => x.AttributeValues)
                .ThenInclude(x => x.AttributeDefinition)
                    .ThenInclude(x => x.Category)
            .Include(x => x.AttributeValues)
                .ThenInclude(x => x.AttributeDefinition)
                    .ThenInclude(x => x.Options)
            .Include(x => x.Projects)
                .ThenInclude(x => x.TechnologyTags)
                    .ThenInclude(x => x.TechnologyTag)
            .Include(x => x.Cvs)
                .ThenInclude(x => x.Position)
            .FirstOrDefaultAsync(
                x => x.UserId == userId);
    }

    private async Task<CandidateProfileViewModel>
        BuildViewModelAsync(
            CandidateProfile profile,
            string? attributeSearch,
            int? attributeCategoryId)
    {
        var selectedAttributeIds =
            profile.AttributeValues
                .Select(x => x.AttributeDefinitionId)
                .ToList();

        var availableQuery =
            _context.AttributeDefinitions
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(
                    x => !selectedAttributeIds.Contains(x.Id));

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
                    x => x.LastUsedAt)
                .ThenBy(x => x.Name)
                .Take(50)
                .Select(
                    x =>
                        new AvailableProfileAttributeViewModel
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Category =
                                x.Category.Name,
                            DataType =
                                x.DataType,
                            LastUsedAt =
                                x.LastUsedAt
                        })
                .ToListAsync();

        var categories =
            await _context.AttributeCategories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(
                    x =>
                        new CandidateAttributeCategoryViewModel
                        {
                            Id = x.Id,
                            Name = x.Name
                        })
                .ToListAsync();

        return new CandidateProfileViewModel
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Location = profile.Location,
            PhotoUrl = profile.PhotoUrl,
            Version = profile.Version,

            AttributeSearch =
                attributeSearch,

            AttributeCategoryId =
                attributeCategoryId,

            Attributes =
                profile.AttributeValues
                    .OrderBy(
                        x => x.AttributeDefinition.Name)
                    .Select(
                        x =>
                            new CandidateAttributeViewModel
                            {
                                Id = x.Id,

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
                                    x.AttributeDefinition
                                        .Options
                                        .OrderBy(
                                            o => o.SortOrder)
                                        .Select(
                                            o =>
                                                new CandidateAttributeOptionViewModel
                                                {
                                                    Id = o.Id,
                                                    Value = o.Value
                                                })
                                        .ToList()
                            })
                    .ToList(),

            AvailableAttributes =
                availableAttributes,

            Categories =
                categories,

            Projects =
                profile.Projects
                    .OrderByDescending(
                        x => x.StartDate)
                    .Select(
                        x =>
                            new CandidateProjectViewModel
                            {
                                Id = x.Id,

                                Name = x.Name,

                                Period =
                                    x.EndDate.HasValue
                                        ? $"{x.StartDate:MMM yyyy} - {x.EndDate.Value:MMM yyyy}"
                                        : $"{x.StartDate:MMM yyyy} - Present",

                                DescriptionMarkdown =
                                    x.DescriptionMarkdown,

                                TechnologyTags =
                                    x.TechnologyTags
                                        .OrderBy(
                                            t => t.TechnologyTag.Name)
                                        .Select(
                                            t => t.TechnologyTag.Name)
                                        .ToList()
                            })
                    .ToList(),

            Cvs =
                profile.Cvs
                    .OrderByDescending(
                        x => x.UpdatedAt)
                    .Select(
                        x =>
                            new CandidateCvSummaryViewModel
                            {
                                Id = x.Id,
                                PositionTitle =
                                    x.Position.Title,
                                Title =
                                    x.Title,
                                IsPublished =
                                    x.IsPublished,
                                UpdatedAt =
                                    x.UpdatedAt
                            })
                    .ToList()
        };
    }

    private async Task RebuildListsAsync(
        CandidateProfileViewModel model,
        CandidateProfile profile)
    {
        var rebuilt =
            await BuildViewModelAsync(
                profile,
                model.AttributeSearch,
                model.AttributeCategoryId);

        model.Id = rebuilt.Id;
        model.UserId = rebuilt.UserId;
        model.Version = rebuilt.Version;

        model.Attributes =
            rebuilt.Attributes;

        model.AvailableAttributes =
            rebuilt.AvailableAttributes;

        model.Categories =
            rebuilt.Categories;

        model.Projects =
            rebuilt.Projects;

        model.Cvs =
            rebuilt.Cvs;

        model.IsAdministratorView =
            model.IsAdministratorView;
    }

    private IActionResult RedirectToProfile(
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
                    !string.Equals(
                        currentUserId,
                        userId,
                        StringComparison.Ordinal)
                        ? userId
                        : null
            });
    }
}
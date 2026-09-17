using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvManagementSystem.Services;

namespace CvManagementSystem.Controllers;

public class PositionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PositionAccessService _positionAccessService;

    public PositionsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    PositionAccessService positionAccessService)
     {
       _context = context;
       _userManager = userManager;
       _positionAccessService = positionAccessService;
     }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? tag)
    {
    var userId =
        _userManager.GetUserId(User);

    var unrestricted =
        User.IsInRole("Recruiter") ||
        User.IsInRole("Administrator");

    var positions =
        await _positionAccessService
            .GetVisiblePositionsAsync(
                userId,
                unrestricted);

    var model =
        positions
            .Select(x =>
                new PositionListViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    IsPublic = x.IsPublic,
                    MaxProjects = x.MaxProjects,
                    UpdatedAt = x.UpdatedAt,
                    AttributeCount =
                        x.Attributes.Count,
                    ProjectTagCount =
                        x.ProjectTags.Count
                })
            .ToList();

     return View(model);
   }

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new PositionViewModel();

        await LoadSelectionDataAsync(model);

        return View(model);
    }

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        PositionViewModel model)
    {
        await LoadSelectionDataAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var title =
            model.Title.Trim();

        var position = new Position
        {
            Title = title,

            Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim(),

            IsPublic =
                model.IsPublic,

            MaxProjects =
                model.MaxProjects,

            CreatedAt =
                DateTime.UtcNow,

            UpdatedAt =
                DateTime.UtcNow,

            Version =
                Guid.NewGuid()
        };

        await ConfigurePositionAsync(
            position,
            model);

        _context.Positions.Add(position);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Position '{position.Title}' was created.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(x => x.Attributes)
                .ThenInclude(x => x.AttributeDefinition)
            .Include(x => x.ProjectTags)
                .ThenInclude(x => x.TechnologyTag)
            .Include(x => x.AccessRules)
                .ThenInclude(x => x.AttributeDefinition)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (position == null)
        {
            return NotFound();
        }

        var model = new PositionViewModel
        {
            Id = position.Id,

            Title = position.Title,

            Description = position.Description,

            IsPublic = position.IsPublic,

            MaxProjects = position.MaxProjects,

            Version = position.Version,

            SelectedAttributeIds =
                position.Attributes
                    .OrderBy(x => x.SortOrder)
                    .Select(x => x.AttributeDefinitionId)
                    .ToList(),

            RequiredAttributeIds =
                position.Attributes
                    .Where(x => x.IsRequired)
                    .Select(x => x.AttributeDefinitionId)
                    .ToList(),

            SelectedTechnologyTagIds =
                position.ProjectTags
                    .Select(x => x.TechnologyTagId)
                    .ToList(),

            AccessRules =
                position.AccessRules
                    .OrderBy(x => x.Id)
                    .Select(
                        x => new PositionAccessRuleViewModel
                        {
                            AttributeDefinitionId =
                                x.AttributeDefinitionId,

                            AttributeName =
                                x.AttributeDefinition.Name,

                            DataType =
                                x.AttributeDefinition.DataType,

                            Operator =
                                x.Operator,

                            Value =
                                x.Value
                        })
                    .ToList()
        };

        await LoadSelectionDataAsync(model);

        return View(model);
    }

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        PositionViewModel model)
    {
        await LoadSelectionDataAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var position = await _context.Positions
            .Include(x => x.Attributes)
            .Include(x => x.ProjectTags)
            .Include(x => x.AccessRules)
            .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (position == null)
        {
            return NotFound();
        }

        if (position.Version != model.Version)
        {
            ModelState.AddModelError(
                string.Empty,
                "This position was changed by another recruiter. " +
                "Reload the page before editing it again.");

            return View(model);
        }

        position.Title =
            model.Title.Trim();

        position.Description =
            string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();

        position.IsPublic =
            model.IsPublic;

        position.MaxProjects =
            model.MaxProjects;

        position.UpdatedAt =
            DateTime.UtcNow;

        position.Version =
            Guid.NewGuid();

        _context.PositionAttributes
            .RemoveRange(position.Attributes);

        _context.PositionProjectTags
            .RemoveRange(position.ProjectTags);

        _context.PositionAccessRules
            .RemoveRange(position.AccessRules);

        position.Attributes.Clear();
        position.ProjectTags.Clear();
        position.AccessRules.Clear();

        await ConfigurePositionAsync(
            position,
            model);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The position was changed by another recruiter. " +
                "Reload the page and try again.");

            return View(model);
        }

        TempData["Success"] =
            $"Position '{position.Title}' was updated.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            TempData["Error"] =
                "Select at least one position.";

            return RedirectToAction(nameof(Index));
        }

        var positions = await _context.Positions
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync();

        _context.Positions.RemoveRange(positions);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"{positions.Count} position(s) deleted.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpGet]
    public async Task<IActionResult> Duplicate(int id)
    {
        var source = await _context.Positions
            .AsNoTracking()
            .Include(x => x.Attributes)
            .Include(x => x.ProjectTags)
            .Include(x => x.AccessRules)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (source == null)
        {
            return NotFound();
        }

        var duplicate = new Position
        {
            Title = $"{source.Title} Copy",

            Description =
                source.Description,

            IsPublic =
                source.IsPublic,

            MaxProjects =
                source.MaxProjects,

            CreatedAt =
                DateTime.UtcNow,

            UpdatedAt =
                DateTime.UtcNow,

            Version =
                Guid.NewGuid()
        };

        duplicate.Attributes =
            source.Attributes
                .Select(
                    x => new PositionAttribute
                    {
                        Position = duplicate,
                        AttributeDefinitionId =
                            x.AttributeDefinitionId,
                        SortOrder =
                            x.SortOrder,
                        IsRequired =
                            x.IsRequired
                    })
                .ToList();

        duplicate.ProjectTags =
            source.ProjectTags
                .Select(
                    x => new PositionProjectTag
                    {
                        Position = duplicate,
                        TechnologyTagId =
                            x.TechnologyTagId
                    })
                .ToList();

        duplicate.AccessRules =
            source.AccessRules
                .Select(
                    x => new PositionAccessRule
                    {
                        Position = duplicate,
                        AttributeDefinitionId =
                            x.AttributeDefinitionId,
                        Operator =
                            x.Operator,
                        Value =
                            x.Value
                    })
                .ToList();

        _context.Positions.Add(duplicate);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Position '{source.Title}' was duplicated.";

        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(x => x.Attributes)
                .ThenInclude(x => x.AttributeDefinition)
                    .ThenInclude(x => x.Category)
            .Include(x => x.ProjectTags)
                .ThenInclude(x => x.TechnologyTag)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (position == null)
        {
            return NotFound();
        }

        var model = new PositionDetailsViewModel
        {
            Id = position.Id,

            Title = position.Title,

            Description =
                position.Description,

            IsPublic =
                position.IsPublic,

            MaxProjects =
                position.MaxProjects,

            Attributes =
                position.Attributes
                    .OrderBy(x => x.SortOrder)
                    .Select(
                        x => new PositionAttributeListViewModel
                        {
                            Name =
                                x.AttributeDefinition.Name,

                            Category =
                                x.AttributeDefinition
                                    .Category.Name,

                            DataType =
                                x.AttributeDefinition
                                    .DataType,

                            IsRequired =
                                x.IsRequired
                        })
                    .ToList(),

            TechnologyTags =
                position.ProjectTags
                    .OrderBy(
                        x => x.TechnologyTag.Name)
                    .Select(
                        x => x.TechnologyTag.Name)
                    .ToList()
        };


        ViewBag.CanGenerateCv = false;
        ViewBag.ExistingCvId = (int?)null;

        var currentUserId = _userManager.GetUserId(User);

        if (User.IsInRole("Candidate") &&
             !string.IsNullOrWhiteSpace(currentUserId))
        {
              var existingCv = await _context.Cvs
                 .AsNoTracking()
                 .Where(x =>
                      x.PositionId == id &&
                      x.CandidateProfile.UserId == currentUserId)
                 .Select(x => new
                {
                      x.Id
                })
                 .FirstOrDefaultAsync();

            if (existingCv != null)
                 {
                      ViewBag.ExistingCvId = existingCv.Id;
                  }
            else
            {
                 ViewBag.CanGenerateCv =
                     await _positionAccessService.CanAccessAsync(
                          id,
                          currentUserId);
            }
        }

        return View(model);
    }

    private async Task ConfigurePositionAsync(
        Position position,
        PositionViewModel model)
    {
        var selectedAttributeIds =
            model.SelectedAttributeIds
                .Distinct()
                .ToList();

        if (selectedAttributeIds.Count > 0)
        {
            var definitions =
                await _context.AttributeDefinitions
                    .AsNoTracking()
                    .Where(
                        x =>
                            selectedAttributeIds
                                .Contains(x.Id))
                    .OrderBy(x => x.Name)
                    .ToListAsync();

            var requiredIds =
                model.RequiredAttributeIds
                    .ToHashSet();

            for (var i = 0;
                 i < definitions.Count;
                 i++)
            {
                var definition =
                    definitions[i];

                position.Attributes.Add(
                    new PositionAttribute
                    {
                        Position =
                            position,

                        AttributeDefinitionId =
                            definition.Id,

                        SortOrder =
                            i,

                        IsRequired =
                            requiredIds.Contains(
                                definition.Id)
                    });
            }
        }

        var selectedTagIds =
            model.SelectedTechnologyTagIds
                .Distinct()
                .ToList();

        if (selectedTagIds.Count > 0)
        {
            var tags =
                await _context.TechnologyTags
                    .AsNoTracking()
                    .Where(
                        x =>
                            selectedTagIds
                                .Contains(x.Id))
                    .ToListAsync();

            position.ProjectTags =
                tags
                    .Select(
                        x =>
                            new PositionProjectTag
                            {
                                Position =
                                    position,

                                TechnologyTagId =
                                    x.Id
                            })
                    .ToList();
        }

        if (model.AccessRules.Count > 0)
        {
            var ruleAttributeIds =
                model.AccessRules
                    .Select(
                        x =>
                            x.AttributeDefinitionId)
                    .Distinct()
                    .ToList();

            var definitions =
                await _context.AttributeDefinitions
                    .AsNoTracking()
                    .Where(
                        x =>
                            ruleAttributeIds
                                .Contains(x.Id))
                    .ToDictionaryAsync(
                        x => x.Id);

            foreach (var rule in model.AccessRules)
            {
                if (!definitions.TryGetValue(
                        rule.AttributeDefinitionId,
                        out var definition))
                {
                    continue;
                }

                if (!IsOperatorAllowed(
                        definition.DataType,
                        rule.Operator))
                {
                    continue;
                }

                position.AccessRules.Add(
                    new PositionAccessRule
                    {
                        Position =
                            position,

                        AttributeDefinitionId =
                            definition.Id,

                        Operator =
                            rule.Operator,

                        Value =
                            rule.Value.Trim()
                    });
            }
        }
    }

    private async Task LoadSelectionDataAsync(
        PositionViewModel model)
    {
        model.AvailableAttributes =
            await _context.AttributeDefinitions
                .AsNoTracking()
                .Include(x => x.Category)
                .OrderBy(x => x.Name)
                .Select(
                    x => new SelectableAttributeViewModel
                    {
                        Id =
                            x.Id,

                        Name =
                            x.Name,

                        Category =
                            x.Category.Name,

                        DataType =
                            x.DataType
                    })
                .ToListAsync();

        model.AvailableTechnologyTags =
            await _context.TechnologyTags
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(
                    x =>
                        new SelectableTechnologyTagViewModel
                        {
                            Id =
                                x.Id,

                            Name =
                                x.Name
                        })
                .ToListAsync();

        model.AttributeCategories =
            await _context.AttributeCategories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(
                    x =>
                        new SelectableCategoryViewModel
                        {
                            Id =
                                x.Id,

                            Name =
                                x.Name
                        })
                .ToListAsync();
    }

    private static bool IsOperatorAllowed(
        AttributeDataType dataType,
        AccessRuleOperator op)
    {
        return dataType switch
        {
            AttributeDataType.String =>
                op is AccessRuleOperator.Equals
                    or AccessRuleOperator.NotEquals
                    or AccessRuleOperator.Contains
                    or AccessRuleOperator.StartsWith,

            AttributeDataType.Text =>
                op is AccessRuleOperator.Equals
                    or AccessRuleOperator.NotEquals
                    or AccessRuleOperator.Contains,

            AttributeDataType.Numeric =>
                op is AccessRuleOperator.Equals
                    or AccessRuleOperator.NotEquals
                    or AccessRuleOperator.GreaterThan
                    or AccessRuleOperator.GreaterThanOrEqual
                    or AccessRuleOperator.LessThan
                    or AccessRuleOperator.LessThanOrEqual,

            AttributeDataType.Date =>
                op is AccessRuleOperator.Equals
                    or AccessRuleOperator.NotEquals
                    or AccessRuleOperator.GreaterThan
                    or AccessRuleOperator.GreaterThanOrEqual
                    or AccessRuleOperator.LessThan
                    or AccessRuleOperator.LessThanOrEqual,

            AttributeDataType.Boolean =>
                op is AccessRuleOperator.Equals
                    or AccessRuleOperator.NotEquals,

            AttributeDataType.Dropdown =>
                op is AccessRuleOperator.Equals
                    or AccessRuleOperator.NotEquals
                    or AccessRuleOperator.Contains,

            AttributeDataType.Image =>
                op is AccessRuleOperator.Equals
                    or AccessRuleOperator.NotEquals,

            AttributeDataType.Period =>
                op is AccessRuleOperator.Equals
                    or AccessRuleOperator.NotEquals,

            _ => false
        };
    }
}

public class PositionListViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public int MaxProjects { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int AttributeCount { get; set; }

    public int ProjectTagCount { get; set; }
}

public class PositionDetailsViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public int MaxProjects { get; set; }

    public List<PositionAttributeListViewModel> Attributes { get; set; }
        = new();

    public List<string> TechnologyTags { get; set; }
        = new();
}

public class PositionAttributeListViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public bool IsRequired { get; set; }
}
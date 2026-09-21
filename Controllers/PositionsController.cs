using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using CvManagementSystem.ViewModels;
using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? tag)
    {
        var currentUser = await _userManager.GetUserAsync(User);

        var isRecruiter =
            currentUser != null &&
            await _userManager.IsInRoleAsync(
                currentUser,
                "Recruiter");

        var isAdministrator =
            currentUser != null &&
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");

        var positions =
            await _positionAccessService.GetVisiblePositionsAsync(
                currentUser?.Id,
                isRecruiter || isAdministrator);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();

            positions = positions
                .Where(x =>
                    x.Title.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase) ||
                    (
                        x.Description?.Contains(
                            normalizedSearch,
                            StringComparison.OrdinalIgnoreCase)
                        ?? false))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim();

            positions = positions
                .Where(x =>
                    x.ProjectTags.Any(projectTag =>
                        string.Equals(
                            projectTag.TechnologyTag.Name,
                            normalizedTag,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var positionIds = positions
            .Select(x => x.Id)
            .ToList();

        var cvCounts = positionIds.Count == 0
            ? new Dictionary<int, int>()
            : await _context.Cvs
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

        var model = positions
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new PositionListViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                IsPublic = x.IsPublic,
                MaxProjects = x.MaxProjects,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Version = x.Version,

                SubmittedCvCount =
                    cvCounts.GetValueOrDefault(x.Id),

                TechnologyTags =
                    x.ProjectTags
                        .OrderBy(y => y.TechnologyTag.Name)
                        .Select(y => y.TechnologyTag.Name)
                        .ToList()
            })
            .ToList();

        ViewBag.Search = search;
        ViewBag.Tag = tag;
        ViewBag.IsRecruiter = isRecruiter;
        ViewBag.IsAdministrator = isAdministrator;

        return View(model);
    }

    // =========================================================
    // DETAILS
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(x => x.Attributes)
                .ThenInclude(x => x.AttributeDefinition)
                    .ThenInclude(x => x.Category)
            .Include(x => x.Attributes)
                .ThenInclude(x => x.AttributeDefinition)
                    .ThenInclude(x => x.Options)
            .Include(x => x.ProjectTags)
                .ThenInclude(x => x.TechnologyTag)
            .Include(x => x.AccessRules)
                .ThenInclude(x => x.AttributeDefinition)
                    .ThenInclude(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (position == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);

        var isRecruiter =
            currentUser != null &&
            await _userManager.IsInRoleAsync(
                currentUser,
                "Recruiter");

        var isAdministrator =
            currentUser != null &&
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");

        if (!position.IsPublic &&
            !isRecruiter &&
            !isAdministrator)
        {
            if (currentUser == null)
            {
                return Forbid();
            }

            var canAccess =
                await _positionAccessService.CanAccessAsync(
                    id,
                    currentUser.Id);

            if (!canAccess)
            {
                return Forbid();
            }
        }

        var model = new PositionDetailsViewModel
        {
            Id = position.Id,
            Title = position.Title,
            Description = position.Description,
            IsPublic = position.IsPublic,
            MaxProjects = position.MaxProjects,
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt,
            Version = position.Version,

            Attributes = position.Attributes
                .OrderBy(x => x.SortOrder)
                .Select(x => new PositionAttributeListViewModel
                {
                    AttributeDefinitionId =
                        x.AttributeDefinitionId,

                    Name =
                        x.AttributeDefinition.Name,

                    Category =
                        x.AttributeDefinition.Category?.Name ??
                        "General",

                    DataType =
                        x.AttributeDefinition.DataType,

                    IsRequired =
                        x.IsRequired,

                    SortOrder =
                        x.SortOrder
                })
                .ToList(),

            ProjectTags = position.ProjectTags
                .OrderBy(x => x.TechnologyTag.Name)
                .Select(x => x.TechnologyTag.Name)
                .ToList(),

            AccessRules = position.AccessRules
                .OrderBy(x => x.AttributeDefinition.Name)
                .Select(x => new PositionAccessRuleViewModel
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

        // -----------------------------------------------------
        // Candidate CV generation state
        // -----------------------------------------------------

        ViewBag.CanGenerateCv = false;
        ViewBag.ExistingCvId = (int?)null;

        if (User.IsInRole("Candidate") &&
            currentUser != null)
        {
            var existingCv = await _context.Cvs
                .AsNoTracking()
                .Where(x =>
                    x.PositionId == id &&
                    x.CandidateProfile.UserId ==
                    currentUser.Id)
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
                        currentUser.Id);
            }
        }

        // -----------------------------------------------------
        // CVs for Recruiters/Admins
        // -----------------------------------------------------

        if (isRecruiter || isAdministrator)
        {
            model.Cvs = await _context.Cvs
                .AsNoTracking()
                .Where(x =>
                    x.PositionId == id &&
                    x.IsPublished)
                .Include(x => x.CandidateProfile)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new PositionCvSummaryViewModel
                {
                    Id = x.Id,

                    Title = x.Title,

                    CandidateName =
                        (
                            x.CandidateProfile.FirstName +
                            " " +
                            x.CandidateProfile.LastName
                        ).Trim(),

                    IsPublished =
                        x.IsPublished,

                    UpdatedAt =
                        x.UpdatedAt,

                    LikeCount =
                        _context.CvLikes.Count(
                            like => like.CvId == x.Id)
                })
                .ToListAsync();
        }

        // -----------------------------------------------------
        // Discussion
        //
        // IMPORTANT:
        // First execute the EF query with ToListAsync().
        // Only after that do we call Markdown.ToHtml().
        // This fixes CS0854.
        // -----------------------------------------------------

        var discussionEntities =
            await _context.PositionDiscussions
                .AsNoTracking()
                .Where(x => x.PositionId == id)
                .Include(x => x.Author)
                .ThenInclude(x => x.CandidateProfile)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

        var markdownPipeline =
            new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .DisableHtml()
                .Build();

        var discussions =
            discussionEntities
                .Select(x => new PositionDiscussionViewModel
                {
                    Id = x.Id,

                    AuthorId = x.AuthorId,

                    AuthorName =
                        x.Author.CandidateProfile != null
                            ? (
                                x.Author.CandidateProfile.FirstName +
                                " " +
                                x.Author.CandidateProfile.LastName
                              ).Trim()
                            : (
                                x.Author.UserName ??
                                x.Author.Email ??
                                "User"
                              ),

                    MessageMarkdown =
                        x.MessageMarkdown,

                    RenderedHtml =
                        Markdown.ToHtml(
                            x.MessageMarkdown,
                            markdownPipeline),

                    CreatedAt =
                        x.CreatedAt
                })
                .ToList();

        ViewBag.Discussions = discussions;

        ViewBag.CanPostDiscussion =
            currentUser != null;

        ViewBag.ShowDiscussionProfileLinks =
            isRecruiter || isAdministrator;

        ViewBag.IsRecruiter = isRecruiter;
        ViewBag.IsAdministrator = isAdministrator;

        return View(model);
    }

    // =========================================================
    // CREATE GET
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new PositionViewModel();

        await LoadSelectionDataAsync(model);

        return View(model);
    }

    // =========================================================
    // CREATE POST
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        PositionViewModel model)
    {
        NormalizeModel(model);

        if (!ValidatePositionModel(model))
        {
            await LoadSelectionDataAsync(model);
            return View(model);
        }

        var position = new Position
        {
            Title = model.Title.Trim(),
            Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim(),

            IsPublic = model.IsPublic,

            MaxProjects = model.MaxProjects,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = Guid.NewGuid()
        };

        var configurationResult =
            await ConfigurePositionAsync(
                position,
                model);

        if (!configurationResult)
        {
            await LoadSelectionDataAsync(model);
            return View(model);
        }

        _context.Positions.Add(position);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Position created successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = position.Id });
    }

    // =========================================================
    // EDIT GET
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(x => x.Attributes)
            .Include(x => x.ProjectTags)
            .Include(x => x.AccessRules)
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
                    .Select(x =>
                        x.AttributeDefinitionId)
                    .ToList(),

            RequiredAttributeIds =
                position.Attributes
                    .Where(x => x.IsRequired)
                    .Select(x =>
                        x.AttributeDefinitionId)
                    .ToList(),

            SelectedTechnologyTagIds =
                position.ProjectTags
                    .Select(x => x.TechnologyTagId)
                    .ToList(),

            AccessRules =
                position.AccessRules
                    .OrderBy(x =>
                        x.AttributeDefinitionId)
                    .Select(x =>
                        new PositionAccessRuleViewModel
                        {
                            AttributeDefinitionId =
                                x.AttributeDefinitionId,

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

    // =========================================================
    // EDIT POST
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        PositionViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        NormalizeModel(model);

        var position = await _context.Positions
            .Include(x => x.Attributes)
            .Include(x => x.ProjectTags)
            .Include(x => x.AccessRules)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (position == null)
        {
            return NotFound();
        }

        if (position.Version != model.Version)
        {
            ModelState.AddModelError(
                string.Empty,
                "This position was changed by another user. Reload the page and try again.");

            await LoadSelectionDataAsync(model);

            return View(model);
        }

        if (!ValidatePositionModel(model))
        {
            await LoadSelectionDataAsync(model);
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

        position.Attributes.Clear();
        position.ProjectTags.Clear();
        position.AccessRules.Clear();

        var configurationResult =
            await ConfigurePositionAsync(
                position,
                model);

        if (!configurationResult)
        {
            await LoadSelectionDataAsync(model);
            return View(model);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Position updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = position.Id });
    }

    // =========================================================
    // EXPORT CVS (optional feature: aggregate CSV export)
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpGet]
    public async Task<IActionResult> ExportCvs(int id)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(x => x.Attributes)
                .ThenInclude(x => x.AttributeDefinition)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (position == null)
        {
            return NotFound();
        }

        var orderedAttributes = position.Attributes
            .OrderBy(x => x.SortOrder)
            .Select(x => x.AttributeDefinition)
            .ToList();

        var cvs = await _context.Cvs
            .AsNoTracking()
            .Where(x => x.PositionId == id && x.IsPublished)
            .Include(x => x.CandidateProfile)
            .OrderBy(x => x.CandidateProfile.LastName)
            .ThenBy(x => x.CandidateProfile.FirstName)
            .ToListAsync();

        // Single batched query for every CV's attribute values instead of
        // one query per CV/attribute pair inside a loop.
        var cvIds = cvs.Select(x => x.Id).ToList();

        var attributeValues = cvIds.Count == 0
            ? new List<CvAttributeValue>()
            : await _context.CvAttributeValues
                .AsNoTracking()
                .Where(x => cvIds.Contains(x.CvId))
                .ToListAsync();

        var valuesByCv = attributeValues
            .GroupBy(x => x.CvId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.AttributeDefinitionId, x => x.Value));

        var likeCounts = cvIds.Count == 0
            ? new Dictionary<int, int>()
            : await _context.CvLikes
                .AsNoTracking()
                .Where(x => cvIds.Contains(x.CvId))
                .GroupBy(x => x.CvId)
                .Select(g => new { CvId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CvId, x => x.Count);

        var sb = new System.Text.StringBuilder();

        var header = new List<string>
        {
            "Candidate Name",
            "CV Title",
            "Updated",
            "Likes"
        };

        header.AddRange(orderedAttributes.Select(a => a.Name));

        sb.AppendLine(string.Join(",", header.Select(CsvEscape)));

        foreach (var cv in cvs)
        {
            var row = new List<string>
            {
                $"{cv.CandidateProfile.FirstName} {cv.CandidateProfile.LastName}".Trim(),
                cv.Title,
                cv.UpdatedAt.ToString("yyyy-MM-dd"),
                likeCounts.GetValueOrDefault(cv.Id).ToString()
            };

            var cvValues = valuesByCv.GetValueOrDefault(cv.Id)
                ?? new Dictionary<int, string?>();

            row.AddRange(
                orderedAttributes.Select(
                    a => cvValues.GetValueOrDefault(a.Id) ?? string.Empty));

            sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
        }

        var bytes = new System.Text.UTF8Encoding(true).GetBytes(sb.ToString());

        var fileName =
            $"{position.Title.Replace(" ", "-")}-cvs.csv";

        return File(bytes, "text/csv", fileName);
    }

    private static string CsvEscape(string? value)
    {
        value ??= string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    // =========================================================
    // DUPLICATE
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
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
            Title = source.Title + " (Copy)",

            Description =
                source.Description,

            IsPublic =
                source.IsPublic,

            MaxProjects =
                source.MaxProjects,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow,

            Version = Guid.NewGuid()
        };

        duplicate.Attributes =
            source.Attributes
                .Select(x => new PositionAttribute
                {
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
                .Select(x =>
                    new PositionProjectTag
                    {
                        TechnologyTagId =
                            x.TechnologyTagId
                    })
                .ToList();

        duplicate.AccessRules =
            source.AccessRules
                .Select(x =>
                    new PositionAccessRule
                    {
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

        TempData["SuccessMessage"] =
            "Position duplicated successfully.";

        return RedirectToAction(
            nameof(Edit),
            new { id = duplicate.Id });
    }

    // =========================================================
    // DELETE SELECTED
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected(
        int[] selectedIds)
    {
        var ids = selectedIds?
            .Where(x => x > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();

        if (ids.Count == 0)
        {
            TempData["ErrorMessage"] =
                "Select at least one position.";

            return RedirectToAction(
                nameof(Index));
        }

        var positions = await _context.Positions
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();

        if (positions.Count == 0)
        {
            TempData["ErrorMessage"] =
                "No matching positions were found.";

            return RedirectToAction(
                nameof(Index));
        }

        _context.Positions.RemoveRange(positions);

        try
        {
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{positions.Count} position(s) deleted successfully.";
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "One or more positions cannot be deleted because they are already referenced by existing CVs.";
        }

        return RedirectToAction(
            nameof(Index));
    }

    // =========================================================
    // ADD DISCUSSION
    // =========================================================

    [Authorize(Roles = "Candidate,Recruiter,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDiscussion(
        int positionId,
        string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["ErrorMessage"] =
                "Discussion message cannot be empty.";

            return RedirectToAction(
                nameof(Details),
                new { id = positionId });
        }

        message = message.Trim();

        if (message.Length > 10000)
        {
            TempData["ErrorMessage"] =
                "Discussion message is too long.";

            return RedirectToAction(
                nameof(Details),
                new { id = positionId });
        }

        var position = await _context.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == positionId);

        if (position == null)
        {
            return NotFound();
        }

        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var isAdmin =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");

        var isRecruiter =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Recruiter");

        if (!position.IsPublic &&
            !isAdmin &&
            !isRecruiter)
        {
            var canAccess =
                await _positionAccessService.CanAccessAsync(
                    positionId,
                    currentUser.Id);

            if (!canAccess)
            {
                return Forbid();
            }
        }

        var discussion = new PositionDiscussion
        {
            PositionId =
                positionId,

            AuthorId =
                currentUser.Id,

            MessageMarkdown =
                message,

            CreatedAt =
                DateTime.UtcNow
        };

        _context.PositionDiscussions.Add(
            discussion);

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Details),
            new { id = positionId });
    }

    // =========================================================
    // DISCUSSION FEED
    // =========================================================

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> DiscussionFeed(
        int id)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (position == null)
        {
            return NotFound();
        }

        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var isAdmin =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");

        var isRecruiter =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Recruiter");

        if (!position.IsPublic &&
            !isAdmin &&
            !isRecruiter)
        {
            var canAccess =
                await _positionAccessService.CanAccessAsync(
                    id,
                    currentUser.Id);

            if (!canAccess)
            {
                return Forbid();
            }
        }

        /*
         * IMPORTANT:
         * EF only executes the database query here.
         * Markdown is processed afterwards in memory.
         */
        var discussionEntities =
            await _context.PositionDiscussions
                .AsNoTracking()
                .Where(x => x.PositionId == id)
                .Include(x => x.Author)
                .ThenInclude(x => x.CandidateProfile)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

        var markdownPipeline =
            new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .DisableHtml()
                .Build();

        var result =
            discussionEntities
                .Select(x => new
                {
                    x.Id,

                    x.AuthorId,

                    AuthorName =
                        x.Author.CandidateProfile != null
                            ? (
                                x.Author.CandidateProfile.FirstName +
                                " " +
                                x.Author.CandidateProfile.LastName
                              ).Trim()
                            : (
                                x.Author.UserName ??
                                x.Author.Email ??
                                "User"
                              ),

                    RenderedHtml =
                        Markdown.ToHtml(
                            x.MessageMarkdown,
                            markdownPipeline),

                    CreatedAt =
                        x.CreatedAt
                            .ToLocalTime()
                            .ToString(
                                "dd MMM yyyy, HH:mm")
                })
                .ToList();

        return Json(result);
    }

    // =========================================================
    // PRIVATE: LOAD SELECTION DATA
    // =========================================================

    private async Task LoadSelectionDataAsync(
        PositionViewModel model)
    {
        var attributes = await _context.AttributeDefinitions
            .AsNoTracking()
            .Include(x => x.Category)
            .OrderBy(x => x.Category.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var categories = await _context.AttributeCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var technologyTags = await _context.TechnologyTags
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        model.AvailableAttributes =
            attributes
                .Select(x =>
                    new SelectableAttributeViewModel
                    {
                        Id = x.Id,

                        Name = x.Name,

                        Category =
                            x.Category?.Name ??
                            "General",

                        DataType =
                            x.DataType
                    })
                .ToList();

        model.AvailableTechnologyTags =
            technologyTags
                .Select(x =>
                    new SelectableTechnologyTagViewModel
                    {
                        Id = x.Id,

                        Name = x.Name
                    })
                .ToList();

        model.AttributeCategories =
            categories
                .Select(x =>
                    new SelectableCategoryViewModel
                    {
                        Id = x.Id,

                        Name = x.Name
                    })
                .ToList();
    }

    // =========================================================
    // PRIVATE: CONFIGURE POSITION
    // =========================================================

    private async Task<bool> ConfigurePositionAsync(
        Position position,
        PositionViewModel model)
    {
        var selectedAttributeIds =
            model.SelectedAttributeIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

        var selectedTechnologyTagIds =
            model.SelectedTechnologyTagIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

        var requiredAttributeIds =
            model.RequiredAttributeIds
                .Where(x => x > 0)
                .Distinct()
                .ToHashSet();

        if (requiredAttributeIds.Any(
                requiredId =>
                    !selectedAttributeIds.Contains(
                        requiredId)))
        {
            ModelState.AddModelError(
                string.Empty,
                "Every required attribute must also be selected.");

            return false;
        }

        var attributeDefinitions =
            selectedAttributeIds.Count == 0
                ? new List<AttributeDefinition>()
                : await _context.AttributeDefinitions
                    .Include(x => x.Category)
                    .Include(x => x.Options)
                    .Where(x =>
                        selectedAttributeIds.Contains(
                            x.Id))
                    .ToListAsync();

        if (attributeDefinitions.Count !=
            selectedAttributeIds.Count)
        {
            ModelState.AddModelError(
                string.Empty,
                "One or more selected attributes no longer exist.");

            return false;
        }

        var technologyTags =
            selectedTechnologyTagIds.Count == 0
                ? new List<TechnologyTag>()
                : await _context.TechnologyTags
                    .Where(x =>
                        selectedTechnologyTagIds.Contains(
                            x.Id))
                    .ToListAsync();

        if (technologyTags.Count !=
            selectedTechnologyTagIds.Count)
        {
            ModelState.AddModelError(
                string.Empty,
                "One or more selected technology tags no longer exist.");

            return false;
        }

        // -----------------------------------------------------
        // Position attributes
        // -----------------------------------------------------

        var sortOrder = 0;

        foreach (var attributeId in selectedAttributeIds)
        {
            position.Attributes.Add(
                new PositionAttribute
                {
                    AttributeDefinitionId =
                        attributeId,

                    SortOrder =
                        sortOrder++,

                    IsRequired =
                        requiredAttributeIds.Contains(
                            attributeId)
                });
        }

        // -----------------------------------------------------
        // Project tags
        // -----------------------------------------------------

        foreach (var technologyTagId
                 in selectedTechnologyTagIds)
        {
            position.ProjectTags.Add(
                new PositionProjectTag
                {
                    TechnologyTagId =
                        technologyTagId
                });
        }

        // -----------------------------------------------------
        // Access rules
        // -----------------------------------------------------

        var seenRuleAttributeIds =
            new HashSet<int>();

        foreach (var rule in model.AccessRules)
        {
            if (rule.AttributeDefinitionId <= 0)
            {
                continue;
            }

            if (seenRuleAttributeIds.Contains(
                    rule.AttributeDefinitionId))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Each access-rule attribute may only appear once.");

                return false;
            }

            seenRuleAttributeIds.Add(
                rule.AttributeDefinitionId);

            var definition =
                attributeDefinitions.FirstOrDefault(
                    x =>
                        x.Id ==
                        rule.AttributeDefinitionId);

            if (definition == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Access rule attribute {rule.AttributeDefinitionId} is not selected.");
                
                return false;
            }

            if (!IsOperatorAllowed(
                    definition.DataType,
                    rule.Operator))
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"The selected operator is not valid for '{definition.Name}'.");

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    rule.Value))
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Access rule value for '{definition.Name}' cannot be empty.");

                return false;
            }

            position.AccessRules.Add(
                new PositionAccessRule
                {
                    AttributeDefinitionId =
                        definition.Id,

                    Operator =
                        rule.Operator,

                    Value =
                        rule.Value.Trim()
                });
        }

        if (!model.IsPublic &&
            position.AccessRules.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "A restricted position must have at least one access rule.");

            return false;
        }

        return true;
    }

    // =========================================================
    // PRIVATE: MODEL VALIDATION
    // =========================================================

    private static bool ValidatePositionModel(
        PositionViewModel model)
    {
        if (model.MaxProjects < 1 ||
            model.MaxProjects > 20)
        {
            return false;
        }

        if (model.SelectedAttributeIds == null)
        {
            model.SelectedAttributeIds =
                new List<int>();
        }

        if (model.RequiredAttributeIds == null)
        {
            model.RequiredAttributeIds =
                new List<int>();
        }

        if (model.SelectedTechnologyTagIds == null)
        {
            model.SelectedTechnologyTagIds =
                new List<int>();
        }

        if (model.AccessRules == null)
        {
            model.AccessRules =
                new List<PositionAccessRuleViewModel>();
        }

        return true;
    }

    // =========================================================
    // PRIVATE: NORMALIZE MODEL
    // =========================================================

    private static void NormalizeModel(
        PositionViewModel model)
    {
        model.Title =
            model.Title?.Trim() ??
            string.Empty;

        model.Description =
            string.IsNullOrWhiteSpace(
                model.Description)
                ? null
                : model.Description.Trim();

        model.SelectedAttributeIds =
            model.SelectedAttributeIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

        model.RequiredAttributeIds =
            model.RequiredAttributeIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

        model.SelectedTechnologyTagIds =
            model.SelectedTechnologyTagIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

        model.AccessRules ??=
            new List<PositionAccessRuleViewModel>();

        model.AccessRules =
            model.AccessRules
                .Where(x =>
                    x.AttributeDefinitionId > 0)
                .ToList();
    }

    // =========================================================
    // PRIVATE: OPERATOR VALIDATION
    // =========================================================

    private static bool IsOperatorAllowed(
        AttributeDataType dataType,
        AccessRuleOperator @operator)
    {
        return dataType switch
        {
            AttributeDataType.String =>
                @operator == AccessRuleOperator.Equals ||
                @operator == AccessRuleOperator.NotEquals ||
                @operator == AccessRuleOperator.Contains ||
                @operator == AccessRuleOperator.StartsWith,

            AttributeDataType.Text =>
                @operator == AccessRuleOperator.Equals ||
                @operator == AccessRuleOperator.NotEquals ||
                @operator == AccessRuleOperator.Contains ||
                @operator == AccessRuleOperator.StartsWith,

            AttributeDataType.Image =>
                @operator == AccessRuleOperator.Equals ||
                @operator == AccessRuleOperator.NotEquals,

            AttributeDataType.Dropdown =>
                @operator == AccessRuleOperator.Equals ||
                @operator == AccessRuleOperator.NotEquals,

            AttributeDataType.Numeric =>
                @operator == AccessRuleOperator.Equals ||
                @operator == AccessRuleOperator.NotEquals ||
                @operator == AccessRuleOperator.GreaterThan ||
                @operator == AccessRuleOperator.GreaterThanOrEqual ||
                @operator == AccessRuleOperator.LessThan ||
                @operator == AccessRuleOperator.LessThanOrEqual,

            AttributeDataType.Date =>
                @operator == AccessRuleOperator.Equals ||
                @operator == AccessRuleOperator.NotEquals ||
                @operator == AccessRuleOperator.GreaterThan ||
                @operator == AccessRuleOperator.GreaterThanOrEqual ||
                @operator == AccessRuleOperator.LessThan ||
                @operator == AccessRuleOperator.LessThanOrEqual,

            AttributeDataType.Boolean =>
                @operator == AccessRuleOperator.Equals ||
                @operator == AccessRuleOperator.NotEquals,

            AttributeDataType.Period =>
                @operator == AccessRuleOperator.Equals ||
                @operator == AccessRuleOperator.NotEquals,

            _ => false
        };
    }
}


// =============================================================
// POSITION LIST VIEW MODEL
// =============================================================

public class PositionListViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public int MaxProjects { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid Version { get; set; }

    public int SubmittedCvCount { get; set; }

    public List<string> TechnologyTags { get; set; } = new();
}


// =============================================================
// POSITION DETAILS VIEW MODEL
// =============================================================

public class PositionDetailsViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public int MaxProjects { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid Version { get; set; }

    public List<PositionAttributeListViewModel> Attributes
        { get; set; } = new();

    public List<string> ProjectTags
        { get; set; } = new();

    public List<PositionAccessRuleViewModel> AccessRules
        { get; set; } = new();

    public List<PositionCvSummaryViewModel> Cvs
        { get; set; } = new();
}


// =============================================================
// POSITION ATTRIBUTE VIEW MODEL
// =============================================================

public class PositionAttributeListViewModel
{
    public int AttributeDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }
}


// =============================================================
// POSITION CV SUMMARY
// =============================================================

public class PositionCvSummaryViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string CandidateName { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int LikeCount { get; set; }
}
using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers;

[Authorize(Roles = "Recruiter,Administrator")]
public class AttributesController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttributesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId)
    {
        var query = _context.AttributeDefinitions
            .AsNoTracking()
            .Include(x => x.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var prefix = search.Trim();

            query = query.Where(x =>
                EF.Functions.ILike(
                    x.Name,
                    prefix + "%"));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x =>
                x.CategoryId == categoryId.Value);
        }

        var attributes = await query
            .OrderBy(x => x.Name)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;

        ViewBag.Categories = await _context.AttributeCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        return View(attributes);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();

        return View(new AttributeDefinitionViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AttributeDefinitionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        var name = model.Name.Trim();

        var exists = await _context.AttributeDefinitions
            .AnyAsync(x => x.Name == name);

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "An attribute with this name already exists.");

            await LoadCategoriesAsync();
            return View(model);
        }

        var categoryExists = await _context.AttributeCategories
            .AnyAsync(x => x.Id == model.CategoryId);

        if (!categoryExists)
        {
            ModelState.AddModelError(
                nameof(model.CategoryId),
                "Selected category does not exist.");

            await LoadCategoriesAsync();
            return View(model);
        }

        var definition = new AttributeDefinition
        {
            Name = name,
            Description = model.Description?.Trim(),
            CategoryId = model.CategoryId,
            DataType = model.DataType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = Guid.NewGuid()
        };

        _context.AttributeDefinitions.Add(definition);

        AddOptions(
            definition,
            model.OptionsText);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var definition = await _context.AttributeDefinitions
            .AsNoTracking()
            .Include(x => x.Options
                .OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (definition == null)
        {
            return NotFound();
        }

        var model = new AttributeDefinitionViewModel
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            CategoryId = definition.CategoryId,
            DataType = definition.DataType,
            Version = definition.Version,

            OptionsText = string.Join(
                Environment.NewLine,
                definition.Options
                    .Select(x => x.Value))
        };

        await LoadCategoriesAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        AttributeDefinitionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        var definition = await _context.AttributeDefinitions
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (definition == null)
        {
            return NotFound();
        }

        if (definition.Version != model.Version)
        {
            ModelState.AddModelError(
                string.Empty,
                "This attribute was changed by another user. " +
                "Reload the page and try again.");

            await LoadCategoriesAsync();

            return View(model);
        }

        var name = model.Name.Trim();

        var duplicateName = await _context.AttributeDefinitions
            .AnyAsync(x =>
                x.Id != model.Id &&
                x.Name == name);

        if (duplicateName)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "An attribute with this name already exists.");

            await LoadCategoriesAsync();

            return View(model);
        }

        var categoryExists = await _context.AttributeCategories
            .AnyAsync(x => x.Id == model.CategoryId);

        if (!categoryExists)
        {
            ModelState.AddModelError(
                nameof(model.CategoryId),
                "Selected category does not exist.");

            await LoadCategoriesAsync();

            return View(model);
        }

        definition.Name = name;

        definition.Description =
            model.Description?.Trim();

        definition.CategoryId =
            model.CategoryId;

        definition.DataType =
            model.DataType;

        definition.UpdatedAt =
            DateTime.UtcNow;

        definition.Version =
            Guid.NewGuid();

        _context.AttributeOptions
            .RemoveRange(definition.Options);

        AddOptions(
            definition,
            model.OptionsText);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            TempData["Error"] =
                "Select at least one attribute.";

            return RedirectToAction(nameof(Index));
        }

        var definitions = await _context.AttributeDefinitions
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync();

        _context.AttributeDefinitions
            .RemoveRange(definitions);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync()
    {
        ViewBag.Categories =
            await _context.AttributeCategories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();
    }

    private void AddOptions(
        AttributeDefinition definition,
        string? optionsText)
    {
        if (definition.DataType !=
            AttributeDataType.Dropdown)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(optionsText))
        {
            return;
        }

        var options = optionsText
            .Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .Select(
                (value, index) =>
                    new AttributeOption
                    {
                        AttributeDefinition =
                            definition,

                        Value = value,

                        SortOrder = index
                    });

        _context.AttributeOptions
            .AddRange(options);
    }
}
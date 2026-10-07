using System.Security.Claims;
using System.Text.Json;
using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers;

[Authorize]
public class SupportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDropboxService _dropboxService;

    public SupportController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IDropboxService dropboxService)
    {
        _context = context;
        _userManager = userManager;
        _dropboxService = dropboxService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl = null)
    {
        var model = new SupportTicketViewModel
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null,
            Positions = await GetPositionsAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupportTicketViewModel model)
    {
        model.Positions = await GetPositionsAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);

        ApplicationUser? currentUser = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            currentUser = await _userManager.FindByIdAsync(userId);
        }

        var roles = currentUser != null
            ? await _userManager.GetRolesAsync(currentUser)
            : new List<string>();

        var userName =
            User.FindFirstValue(ClaimTypes.Name) ??
            currentUser?.UserName ??
            "Unknown user";

        var email =
            User.FindFirstValue(ClaimTypes.Email) ??
            currentUser?.Email ??
            string.Empty;

        var role = roles.FirstOrDefault()
                   ?? "Unknown";

        string? positionTitle = null;

        if (model.PositionId.HasValue)
        {
            positionTitle = await _context.Positions
                .Where(p => p.Id == model.PositionId.Value)
                .Select(p => p.Title)
                .FirstOrDefaultAsync();
        }

        var pageUrl = BuildPageUrl(model.ReturnUrl);

        var ticket = new
        {
            reportedBy = new
            {
                name = userName,
                email,
                role
            },
            positionTitle,
            pageUrl,
            summary = model.Summary.Trim(),
            priority = model.Priority.ToString(),
            reportedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(
            ticket,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var fileName =
            $"support-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";

        try
        {
            await _dropboxService.UploadJsonAsync(fileName, json);

            TempData["SupportSuccess"] =
                "Your support request was submitted successfully.";

            return RedirectToAction(nameof(Create));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                $"Unable to submit support request: {ex.Message}");

            return View(model);
        }
    }

    private async Task<IEnumerable<SelectListItem>> GetPositionsAsync()
    {
        return await _context.Positions
            .OrderBy(p => p.Title)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Title
            })
            .ToListAsync();
    }

    private string? BuildPageUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return $"{Request.Scheme}://{Request.Host}{returnUrl}";
        }

        var referer = Request.Headers.Referer.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(referer))
        {
            return referer;
        }

        return null;
    }
}
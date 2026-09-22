using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(x => x.Email)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.UserName,
                x.CreatedAt,
                x.IsBlocked
            })
            .ToListAsync();

        var candidateIds =
            (await _userManager.GetUsersInRoleAsync("Candidate"))
            .Select(x => x.Id)
            .ToHashSet();

        var recruiterIds =
            (await _userManager.GetUsersInRoleAsync("Recruiter"))
            .Select(x => x.Id)
            .ToHashSet();

        var administratorIds =
            (await _userManager.GetUsersInRoleAsync("Administrator"))
            .Select(x => x.Id)
            .ToHashSet();

        var model = new AdminUsersViewModel
        {
            Users = users
                .Select(x => new AdminUserRowViewModel
                {
                    Id = x.Id,
                    Email = x.Email ?? string.Empty,
                    UserName = x.UserName,
                    CreatedAt = x.CreatedAt,
                    IsBlocked = x.IsBlocked,

                    Roles = BuildRoles(
                        x.Id,
                        candidateIds,
                        recruiterIds,
                        administratorIds)
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(
        string[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            TempData["Error"] =
                "Select at least one user.";

            return RedirectToAction(nameof(Users));
        }

        var users = await _userManager.Users
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            user.IsBlocked = true;
        }

        await _context.SaveChangesAsync();

        await RefreshAuthenticationForSelectedUsers(users);

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(
        string[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            TempData["Error"] =
                "Select at least one user.";

            return RedirectToAction(nameof(Users));
        }

        var users = await _userManager.Users
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            user.IsBlocked = false;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            TempData["Error"] =
                "Select at least one user.";

            return RedirectToAction(nameof(Users));
        }

        var users = await _userManager.Users
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            var result =
                await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(x => x.Description));

                TempData["Error"] =
                    $"Could not delete {user.Email}: {errors}";

                return RedirectToAction(nameof(Users));
            }
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(
        string[] selectedIds,
        string role)
    {
        if (selectedIds.Length == 0)
        {
            TempData["Error"] =
                "Select at least one user.";

            return RedirectToAction(nameof(Users));
        }

        if (!IsSupportedRole(role))
        {
            TempData["Error"] =
                "Invalid role selected.";

            return RedirectToAction(nameof(Users));
        }

        var users = await _userManager.Users
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(
                    user,
                    role))
            {
                continue;
            }

            var result =
                await _userManager.AddToRoleAsync(
                    user,
                    role);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(x => x.Description));

                TempData["Error"] =
                    $"Could not assign {role} to {user.Email}: {errors}";

                return RedirectToAction(nameof(Users));
            }
        }

        await RefreshAuthenticationForSelectedUsers(users);

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(
        string[] selectedIds,
        string role)
    {
        if (selectedIds.Length == 0)
        {
            TempData["Error"] =
                "Select at least one user.";

            return RedirectToAction(nameof(Users));
        }

        if (!IsSupportedRole(role))
        {
            TempData["Error"] =
                "Invalid role selected.";

            return RedirectToAction(nameof(Users));
        }

        var users = await _userManager.Users
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            if (!await _userManager.IsInRoleAsync(
                    user,
                    role))
            {
                continue;
            }

            var result =
                await _userManager.RemoveFromRoleAsync(
                    user,
                    role);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(x => x.Description));

                TempData["Error"] =
                    $"Could not remove {role} from {user.Email}: {errors}";

                return RedirectToAction(nameof(Users));
            }
        }

        await RefreshAuthenticationForSelectedUsers(users);

        return RedirectToAction(nameof(Users));
    }

    private static List<string> BuildRoles(
        string userId,
        HashSet<string> candidateIds,
        HashSet<string> recruiterIds,
        HashSet<string> administratorIds)
    {
        var roles = new List<string>();

        if (candidateIds.Contains(userId))
        {
            roles.Add("Candidate");
        }

        if (recruiterIds.Contains(userId))
        {
            roles.Add("Recruiter");
        }

        if (administratorIds.Contains(userId))
        {
            roles.Add("Administrator");
        }

        return roles;
    }

    private static bool IsSupportedRole(
        string role)
    {
        return role is
            "Candidate" or
            "Recruiter" or
            "Administrator";
    }

    private async Task RefreshAuthenticationForSelectedUsers(
        List<ApplicationUser> users)
    {
        var currentUserId =
            _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return;
        }

        if (!users.Any(x => x.Id == currentUserId))
        {
            return;
        }

        var currentUser =
            users.First(x => x.Id == currentUserId);

        await _userManager.UpdateSecurityStampAsync(currentUser);

        // UpdateSecurityStampAsync alone only invalidates the stamp for the
        // *next* periodic re-validation of the existing cookie (which can be
        // minutes away). RefreshSignInAsync re-issues the cookie for this
        // session immediately, so a role change (e.g. an admin removing their
        // own Administrator role) takes effect on the very next request.
        await _signInManager.RefreshSignInAsync(currentUser);
    }
}
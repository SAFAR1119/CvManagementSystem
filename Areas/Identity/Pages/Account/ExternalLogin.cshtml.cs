using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CvManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CvManagementSystem.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ExternalLoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ProviderDisplayName { get; set; }

    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    // Step 1: User clicks Google/Facebook on Login page.
    // This starts the external authentication challenge.
    public IActionResult OnPost(
        string provider,
        string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return RedirectToPage("./Login");
        }

        returnUrl ??= Url.Content("~/");

        var redirectUrl =
            Url.Page(
                "./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl });

        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            return RedirectToPage("./Login");
        }

        var properties =
            _signInManager.ConfigureExternalAuthenticationProperties(
                provider,
                redirectUrl);

        return new ChallengeResult(provider, properties);
    }

    // Handles a direct GET to /ExternalLogin.
    public IActionResult OnGet()
    {
        return RedirectToPage("./Login");
    }

    // Step 2: Google/Facebook sends the user back here.
    public async Task<IActionResult> OnGetCallbackAsync(
        string? returnUrl = null,
        string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            ErrorMessage =
                $"Error from external provider: {remoteError}";

            return RedirectToPage(
                "./Login",
                new
                {
                    ReturnUrl = returnUrl,
                    ErrorMessage
                });
        }

        var info =
            await _signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            ErrorMessage =
                "Error loading external login information.";

            return RedirectToPage(
                "./Login",
                new
                {
                    ReturnUrl = returnUrl,
                    ErrorMessage
                });
        }

        ProviderDisplayName =
            info.ProviderDisplayName;

        // Existing external login -> sign in directly.
        var result =
            await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "User logged in with {LoginProvider}.",
                info.LoginProvider);

            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        if (result.IsNotAllowed)
        {
            ErrorMessage =
                "This account is not allowed to sign in.";

            return RedirectToPage(
                "./Login",
                new
                {
                    ReturnUrl = returnUrl,
                    ErrorMessage
                });
        }

        // New external account -> show confirmation page.
        ReturnUrl = returnUrl;

        var email =
            info.Principal.FindFirstValue(
                ClaimTypes.Email);

        if (!string.IsNullOrWhiteSpace(email))
        {
            Input.Email = email;
        }

        return Page();
    }

    // Step 3: Create the local Identity account.
    public async Task<IActionResult> OnPostConfirmationAsync(
        string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            ProviderDisplayName =
                "external provider";

            return Page();
        }

        var info =
            await _signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            ErrorMessage =
                "Error loading external login information.";

            return RedirectToPage(
                "./Login",
                new
                {
                    ReturnUrl = returnUrl,
                    ErrorMessage
                });
        }

        ProviderDisplayName =
            info.ProviderDisplayName;

        var existingUser =
            await _userManager.FindByEmailAsync(Input.Email);

        // If a local account already exists with this email,
        // attach the external login to it.
        if (existingUser != null)
        {
            var existingLogin =
                await _userManager.FindByLoginAsync(
                    info.LoginProvider,
                    info.ProviderKey);

            if (existingLogin == null)
            {
                var addLoginResult =
                    await _userManager.AddLoginAsync(
                        existingUser,
                        info);

                if (!addLoginResult.Succeeded)
                {
                    foreach (var error in addLoginResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return Page();
                }
            }

            await _signInManager.SignInAsync(
                existingUser,
                isPersistent: false);

            _logger.LogInformation(
                "Existing account linked with {LoginProvider}.",
                info.LoginProvider);

            return LocalRedirect(returnUrl);
        }

        // Create a new application user.
        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true
        };

        var createResult =
            await _userManager.CreateAsync(user);

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return Page();
        }

        // New social accounts are Candidates by default.
        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                "Candidate");

        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            await _userManager.DeleteAsync(user);

            return Page();
        }

        // Link Facebook/Google to the Identity user.
        var loginResult =
            await _userManager.AddLoginAsync(
                user,
                info);

        if (!loginResult.Succeeded)
        {
            foreach (var error in loginResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            await _userManager.DeleteAsync(user);

            return Page();
        }

        await _signInManager.SignInAsync(
            user,
            isPersistent: false);

        _logger.LogInformation(
            "Created a new user with {LoginProvider}.",
            info.LoginProvider);

        return LocalRedirect(returnUrl);
    }
}
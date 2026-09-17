using CvManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public string SelectedRole { get; set; } = "Candidate";
    private static readonly string[] RegistrationRoles =
       {
         "Candidate",
          "Recruiter"
        };

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; }
        = new List<AuthenticationScheme>();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage =
                "The password must be at least {2} characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(
            "Password",
            ErrorMessage =
                "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        ExternalLogins =
            (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync(
        string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!RegistrationRoles.Contains(SelectedRole))
          {
             ModelState.AddModelError(
             nameof(SelectedRole),
              "Please select a valid account type.");
           }

        if (!ModelState.IsValid)
        {
            ExternalLogins =
                (await _signInManager
                    .GetExternalAuthenticationSchemesAsync())
                .ToList();

            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            CreatedAt = DateTime.UtcNow,
            IsBlocked = false
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

if (result.Succeeded)
{
    var roleResult = await _userManager.AddToRoleAsync(
        user,
        SelectedRole);

    if (!roleResult.Succeeded)
    {
        foreach (var error in roleResult.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        return Page();
    }

        await _signInManager.SignInAsync(
            user,
            isPersistent: false);

        return LocalRedirect(returnUrl);
    }

    foreach (var error in result.Errors)
    {
        ModelState.AddModelError(string.Empty, error.Description);
    }

    ExternalLogins =
        (await _signInManager
            .GetExternalAuthenticationSchemesAsync())
        .ToList();

    return Page();
  }
}
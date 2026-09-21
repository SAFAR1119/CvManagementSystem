using Microsoft.AspNetCore.Mvc;

namespace CvManagementSystem.Controllers;

/// <summary>
/// Persists the user's light/dark theme choice the same way LanguageController
/// persists the UI culture: a long-lived cookie read on the server so the
/// correct theme is rendered on the very first response (no flash of the
/// wrong theme), and readable/writable from client-side JavaScript for an
/// instant, no-reload toggle.
/// </summary>
public class ThemeController : Controller
{
    public const string CookieName = "cv-theme";

    private static readonly string[] SupportedThemes =
    {
        "light",
        "dark"
    };

    [HttpGet]
    public IActionResult Set(
        string theme,
        string? returnUrl)
    {
        if (!SupportedThemes.Contains(theme))
        {
            theme = "light";
        }

        Response.Cookies.Append(
            CookieName,
            theme,
            new CookieOptions
            {
                Expires =
                    DateTimeOffset.UtcNow.AddYears(1),

                IsEssential = true,

                // Must be readable by client-side JavaScript so the
                // instant toggle button can update the cookie without a
                // full page round trip.
                HttpOnly = false,

                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(
            "Index",
            "Home");
    }
}
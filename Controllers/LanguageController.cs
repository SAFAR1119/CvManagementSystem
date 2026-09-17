using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace CvManagementSystem.Controllers;

public class LanguageController : Controller
{
    private static readonly string[] SupportedCultures =
    {
        "en-US",
        "bn-BD"
    };

    [HttpGet]
    public IActionResult Set(
        string culture,
        string? returnUrl)
    {
        if (!SupportedCultures.Contains(culture))
        {
            culture = "en-US";
        }

        var cookieValue =
            CookieRequestCultureProvider
                .MakeCookieValue(
                    new RequestCulture(culture));

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue,
            new CookieOptions
            {
                Expires =
                    DateTimeOffset.UtcNow.AddYears(1),

                IsEssential = true,

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
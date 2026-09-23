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
                // The language switch endpoint is under /Language. Keep the
                // culture cookie available to every page in the application.
                Path = "/",

                Expires =
                    DateTimeOffset.UtcNow.AddYears(1),

                IsEssential = true,

                HttpOnly = false,

                SameSite = SameSiteMode.Lax
            });

        // This makes the selected culture available to the response as well
        // as every later request that carries the cookie.
        HttpContext.Features.Set<IRequestCultureFeature>(
            new RequestCultureFeature(
                new RequestCulture(culture),
                new CookieRequestCultureProvider()));

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

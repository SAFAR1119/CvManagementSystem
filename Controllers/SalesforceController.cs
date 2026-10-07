using System.Security.Cryptography;
using System.Text.Json;
using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CvManagementSystem.Controllers;


public class SalesforceTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } =
        string.Empty;

    [JsonPropertyName("instance_url")]
    public string InstanceUrl { get; set; } =
        string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } =
        string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } =
        string.Empty;

    [JsonPropertyName("issued_at")]
    public string IssuedAt { get; set; } =
        string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } =
        string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } =
        string.Empty;
}

[Authorize]
public class SalesforceController : Controller
{
    // The pending OAuth request is kept in an encrypted, time-limited
    // cookie rather than in-memory session, so it survives an app restart
    // while the user is signing in on Salesforce.
    private const string OAuthCookieName =
        "CvMaster.SalesforceOAuth";

    private static readonly TimeSpan OAuthLifetime =
        TimeSpan.FromMinutes(15);

    private static readonly ChunkingCookieManager CookieManager =
        new();

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SalesforceService _salesforceService;
    private readonly ITimeLimitedDataProtector _oauthProtector;

    public SalesforceController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SalesforceService salesforceService,
        IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _userManager = userManager;
        _salesforceService = salesforceService;
        _oauthProtector =
            dataProtectionProvider
                .CreateProtector("Salesforce.OAuth")
                .ToTimeLimitedDataProtector();
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        string? userId,
        string? error = null)
    {
        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var isAdministrator =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");

        var targetUserId =
            string.IsNullOrWhiteSpace(userId)
                ? currentUser.Id
                : userId;

        if (!isAdministrator &&
            targetUserId != currentUser.Id)
        {
            return Forbid();
        }

        var targetUser =
            await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == targetUserId);

        if (targetUser == null)
        {
            return NotFound();
        }

        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == targetUserId);

        var roles =
            await _userManager.GetRolesAsync(
                targetUser);

        var firstName =
            profile?.FirstName?.Trim()
            ?? string.Empty;

        var lastName =
            profile?.LastName?.Trim()
            ?? string.Empty;

        var email =
            !string.IsNullOrWhiteSpace(profile?.Email)
                ? profile!.Email!.Trim()
                : targetUser.Email?.Trim()
                  ?? string.Empty;

        var phone =
            !string.IsNullOrWhiteSpace(profile?.PhoneNumber)
                ? profile!.PhoneNumber!.Trim()
                : targetUser.PhoneNumber?.Trim()
                  ?? string.Empty;

        var location =
            profile?.Location?.Trim()
            ?? string.Empty;

        var displayFirstName =
            string.IsNullOrWhiteSpace(firstName)
                ? targetUser.UserName?.Trim()
                    ?? "Site"
                : firstName;

        var displayLastName =
            string.IsNullOrWhiteSpace(lastName)
                ? "User"
                : lastName;

        var displayName =
            $"{displayFirstName} {displayLastName}"
                .Trim();

        var accountName =
            string.IsNullOrWhiteSpace(displayName)
                ? email
                : displayName;

        if (string.IsNullOrWhiteSpace(accountName))
        {
            accountName = "CV Master User";
        }

        var model =
            new SalesforceCreateAccountViewModel
            {
                TargetUserId =
                    targetUserId,

                Role =
                    roles.FirstOrDefault()
                    ?? "User",

                FirstName =
                    displayFirstName,

                LastName =
                    displayLastName,

                Email =
                    email,

                Phone =
                    phone,

                Location =
                    location,

                PersonalPhotoUrl =
                    profile?.PhotoUrl?.Trim()
                    ?? string.Empty,

                AccountName =
                    accountName
            };

        ViewData["Error"] = error;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SalesforceCreateAccountViewModel model)
    {
        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var isAdministrator =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");

        var targetUserId =
            string.IsNullOrWhiteSpace(
                model.TargetUserId)
                ? currentUser.Id
                : model.TargetUserId;

        if (!isAdministrator &&
            targetUserId != currentUser.Id)
        {
            return Forbid();
        }

        var targetUser =
            await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == targetUserId);

        if (targetUser == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(model.LastName))
        {
            ModelState.AddModelError(
                nameof(model.LastName),
                "Last name is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var state =
            _salesforceService.GenerateState();

        var codeVerifier =
            _salesforceService.GenerateCodeVerifier();

        var codeChallenge =
            _salesforceService.GenerateCodeChallenge(
                codeVerifier);

        // Built from the current request, so it is
        // http://localhost:5254/Salesforce/Callback in development and
        // https://cv-management-system-kndu.onrender.com/Salesforce/Callback
        // on Render (the https scheme comes from X-Forwarded-Proto, which
        // Program.cs honours via UseForwardedHeaders).
        var redirectUri =
            Url.Action(
                nameof(Callback),
                "Salesforce",
                null,
                Request.Scheme,
                Request.Host.Value)!;

        var sessionData =
            new SalesforceOAuthSessionData
            {
                State =
                    state,

                InitiatorUserId =
                    currentUser.Id,

                TargetUserId =
                    targetUserId,

                CodeVerifier =
                    codeVerifier,

                RedirectUri =
                    redirectUri,

                Form =
                    model
            };

        CookieManager.AppendResponseCookie(
            HttpContext,
            OAuthCookieName,
            _oauthProtector.Protect(
                JsonSerializer.Serialize(sessionData),
                OAuthLifetime),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                MaxAge = OAuthLifetime
            });

        var authorizationUrl =
            _salesforceService.BuildAuthorizationUrl(
                state,
                codeChallenge,
                redirectUri);

        return Redirect(authorizationUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Callback(
        string? code,
        string? state,
        string? error,
        string? error_description,
        CancellationToken cancellationToken)
    {
        var sessionData =
            ReadOAuthCookie();

        DeleteOAuthCookie();

        if (string.IsNullOrWhiteSpace(state) ||
            sessionData == null ||
            string.IsNullOrWhiteSpace(
                sessionData.CodeVerifier) ||
            !CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(state),
                System.Text.Encoding.UTF8.GetBytes(sessionData.State)))
        {
            return RedirectToAction(
                nameof(Create),
                new
                {
                    userId = sessionData?.TargetUserId,
                    error =
                        "The Salesforce authorization session expired or did not match. " +
                        "Please click \"Continue with Salesforce\" again."
                });
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            ViewData["Error"] =
                $"Salesforce authorization failed: " +
                $"{error_description ?? error}";

            return View(
                "Create",
                sessionData.Form);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            ViewData["Error"] =
                "Salesforce did not return an authorization code.";

            return View(
                "Create",
                sessionData.Form);
        }

        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        var isAdministrator =
            await _userManager.IsInRoleAsync(
                currentUser,
                "Administrator");

        // The flow must finish in the same account that started it.
        if (sessionData.InitiatorUserId != currentUser.Id ||
            (!isAdministrator &&
             sessionData.TargetUserId != currentUser.Id))
        {
            return Forbid();
        }

        try
        {
            var token =
                await _salesforceService.ExchangeCodeAsync(
                    code,
                    sessionData.CodeVerifier,
                    cancellationToken,
                    sessionData.RedirectUri);

            var result =
                await _salesforceService.CreateAccountAndContactAsync(
                    token,
                    sessionData.Form,
                    cancellationToken);

            return View(
                "Success",
                result);
        }
        catch (Exception exception)
{
    Console.Error.WriteLine(
        "========== SALESFORCE CALLBACK ERROR ==========");

    Console.Error.WriteLine(
        exception.ToString());

    Console.Error.WriteLine(
        "================================================");

    ViewData["Error"] =
        exception.Message;

    return View(
        "Create",
        sessionData.Form);
    }
    }

    private SalesforceOAuthSessionData? ReadOAuthCookie()
    {
        var protectedValue =
            CookieManager.GetRequestCookie(
                HttpContext,
                OAuthCookieName);

        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<
                SalesforceOAuthSessionData>(
                _oauthProtector.Unprotect(
                    protectedValue));
        }
        catch (Exception exception)
            when (exception is CryptographicException or JsonException)
        {
            // Expired, tampered with, or written with an old key.
            return null;
        }
    }

    private void DeleteOAuthCookie()
    {
        CookieManager.DeleteCookie(
            HttpContext,
            OAuthCookieName,
            new CookieOptions
            {
                Secure = Request.IsHttps,
                Path = "/"
            });
    }

    private sealed class SalesforceOAuthSessionData
    {
        public string State { get; set; } =
            string.Empty;

        public string InitiatorUserId { get; set; } =
            string.Empty;

        public string TargetUserId { get; set; } =
            string.Empty;

        public string CodeVerifier { get; set; } =
            string.Empty;

        // Token exchange must send the same redirect_uri as the
        // authorization request.
        public string RedirectUri { get; set; } =
            string.Empty;

        public SalesforceCreateAccountViewModel Form { get; set; } =
            new();
    }
}
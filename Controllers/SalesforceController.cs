using System.Text.Json;
using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
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
    private const string SessionPrefix =
        "Salesforce:OAuth:";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SalesforceService _salesforceService;

    public SalesforceController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SalesforceService salesforceService)
    {
        _context = context;
        _userManager = userManager;
        _salesforceService = salesforceService;
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

        var sessionData =
            new SalesforceOAuthSessionData
            {
                TargetUserId =
                    targetUserId,

                CodeVerifier =
                    codeVerifier,

                Form =
                    model
            };

        HttpContext.Session.SetString(
            SessionPrefix + state,
            JsonSerializer.Serialize(
                sessionData));

        var authorizationUrl =
            _salesforceService.BuildAuthorizationUrl(
                state,
                codeChallenge);

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
        if (string.IsNullOrWhiteSpace(state))
        {
            return BadRequest(
                "Missing Salesforce OAuth state.");
        }

        var sessionKey =
            SessionPrefix + state;

        var serialized =
            HttpContext.Session.GetString(sessionKey);

        HttpContext.Session.Remove(sessionKey);

        if (string.IsNullOrWhiteSpace(serialized))
        {
            return BadRequest(
                "The Salesforce authorization session expired. Start the integration again.");
        }

        var sessionData =
            JsonSerializer.Deserialize<
                SalesforceOAuthSessionData>(
                serialized);

        if (sessionData == null ||
            string.IsNullOrWhiteSpace(
                sessionData.CodeVerifier))
        {
            return BadRequest(
                "Invalid Salesforce authorization state.");
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
            return BadRequest(
                "Salesforce did not return an authorization code.");
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

        if (!isAdministrator &&
            sessionData.TargetUserId !=
            currentUser.Id)
        {
            return Forbid();
        }

        try
        {
            var token =
                await _salesforceService.ExchangeCodeAsync(
                    code,
                    sessionData.CodeVerifier,
                    cancellationToken);
                    

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

    private sealed class SalesforceOAuthSessionData
    {
        public string TargetUserId { get; set; } =
            string.Empty;

        public string CodeVerifier { get; set; } =
            string.Empty;

        public SalesforceCreateAccountViewModel Form { get; set; } =
            new();
    }
}
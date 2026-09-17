using CvManagementSystem.Data;
using CvManagementSystem.Middleware;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Configuration
// ============================================================

// User Secrets are used for the Supabase connection string.
builder.Configuration.AddUserSecrets<Program>(optional: true);

// ============================================================
// Localization
// ============================================================

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

// ============================================================
// MVC + Razor Pages
// ============================================================

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddRazorPages();

// ============================================================
// Database
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection is not configured.");
}

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseNpgsql(connectionString);
    });

// ============================================================
// Identity
// ============================================================

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.User.RequireUniqueEmail = true;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ============================================================
// Application Services
// ============================================================

builder.Services.AddScoped<PositionAccessService>();

builder.Services.AddScoped<CvGenerationService>();

// ============================================================
// Build Application
// ============================================================

var app = builder.Build();

// ============================================================
// Supported UI Cultures
// ============================================================

var supportedCultures = new[]
{
    "en-US",
    "bn-BD"
};

var localizationOptions =
    new RequestLocalizationOptions()
        .SetDefaultCulture("en-US")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

// ============================================================
// Error Handling
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

// ============================================================
// HTTP Pipeline
// ============================================================

app.UseHttpsRedirection();

app.UseRequestLocalization(
    localizationOptions);

app.UseRouting();

app.UseAuthentication();

app.UseMiddleware<BlockedUserMiddleware>();

app.UseAuthorization();

// ============================================================
// Static Assets
// ============================================================

app.MapStaticAssets();

// ============================================================
// MVC Routes
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ============================================================
// Identity / Razor Pages
// ============================================================

app.MapRazorPages();

// ============================================================
// Database Seeders
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext =
        services.GetRequiredService<ApplicationDbContext>();

    await IdentitySeeder.SeedRolesAsync(
        services,
        app.Configuration);

    await AttributeLibrarySeeder.SeedAsync(
        dbContext);
}

// ============================================================
// Run
// ============================================================

app.Run();
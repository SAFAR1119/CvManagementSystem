using CvManagementSystem.Data;
using CvManagementSystem.Middleware;
using CvManagementSystem.Models;
using CvManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddRazorPages();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection is not configured.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

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

// External authentication providers
builder.Services
    .AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId =
            builder.Configuration[
                "Authentication:Google:ClientId"]
            ?? string.Empty;

        options.ClientSecret =
            builder.Configuration[
                "Authentication:Google:ClientSecret"]
            ?? string.Empty;
    })
    .AddFacebook(options =>
    {
        options.AppId =
            builder.Configuration[
                "Authentication:Facebook:AppId"]
            ?? string.Empty;

        options.AppSecret =
            builder.Configuration[
                "Authentication:Facebook:AppSecret"]
            ?? string.Empty;
    });

builder.Services.AddScoped<PositionAccessService>();
builder.Services.AddScoped<CvGenerationService>();

var app = builder.Build();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseAuthentication();

app.UseMiddleware<BlockedUserMiddleware>();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext =
        services.GetRequiredService<ApplicationDbContext>();

    await IdentitySeeder.SeedRolesAsync(
        services,
        app.Configuration);

    await AttributeLibrarySeeder.SeedAsync(dbContext);
}

app.Run();
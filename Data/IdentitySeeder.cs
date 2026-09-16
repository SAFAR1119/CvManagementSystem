using Microsoft.AspNetCore.Identity;

namespace CvManagementSystem.Data;

public static class IdentitySeeder
{
    private static readonly string[] Roles =
    {
        "Candidate",
        "Recruiter",
        "Administrator"
    };

    public static async Task SeedRolesAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result =
                await roleManager.CreateAsync(
                    new IdentityRole(role));

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(x => x.Description));

                throw new InvalidOperationException(
                    $"Failed to create role '{role}': {errors}");
            }
        }

        await BootstrapAdministratorAsync(
            services,
            configuration);
    }

    private static async Task BootstrapAdministratorAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var adminEmail =
            configuration["BootstrapAdmin:Email"];

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            return;
        }

        var userManager =
            services.GetRequiredService<
                UserManager<Models.ApplicationUser>>();

        var user =
            await userManager.FindByEmailAsync(
                adminEmail.Trim());

        if (user == null)
        {
            return;
        }

        if (await userManager.IsInRoleAsync(
                user,
                "Administrator"))
        {
            return;
        }

        var result =
            await userManager.AddToRoleAsync(
                user,
                "Administrator");

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(
                $"Failed to bootstrap administrator: {errors}");
        }
    }
}
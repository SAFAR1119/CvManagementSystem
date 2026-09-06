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
        IServiceProvider services)
    {
        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result =
                    await roleManager.CreateAsync(
                        new IdentityRole(role));

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Failed to create role '{role}': {errors}");
                }
            }
        }
    }
}
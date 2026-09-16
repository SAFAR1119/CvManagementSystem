using CvManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Data;

public static class AttributeLibrarySeeder
{
    private static readonly (string Name, string Description)[] Categories =
    {
        ("Certification", "Professional certifications and credentials."),
        ("Domain Knowledge", "Industry, business, or technical domain knowledge."),
        ("Personal Information", "Personal and contact-related information."),
        ("Soft Skills", "Communication, teamwork, leadership, and similar skills."),
        ("Education", "Academic background and education-related information."),
        ("Professional", "Professional experience and career-related information."),
        ("Skills", "Technical and professional skills.")
    };

    public static async Task SeedAsync(
        ApplicationDbContext context)
    {
        var existingNames = await context.AttributeCategories
            .Select(x => x.Name)
            .ToListAsync();

        var existing = existingNames
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newCategories = Categories
            .Where(x => !existing.Contains(x.Name))
            .Select(x =>
                new AttributeCategory
                {
                    Name = x.Name,
                    Description = x.Description
                })
            .ToList();

        if (newCategories.Count == 0)
        {
            return;
        }

        await context.AttributeCategories.AddRangeAsync(
            newCategories);

        await context.SaveChangesAsync();
    }
}
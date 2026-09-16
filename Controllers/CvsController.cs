using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CvManagementSystem.Controllers;

public class CvsController : Controller
{
    [HttpGet]
    public IActionResult Generate(int positionId = 1)
    {
        var model = BuildGeneratedCv(positionId);

        return View("Generate", model);
    }

    private static GeneratedCvViewModel BuildGeneratedCv(int positionId)
    {
        var attributes = new List<GeneratedCvAttributeViewModel>
        {
            new()
            {
                AttributeDefinitionId = 1,
                Name = "Professional Summary",
                Category = "Professional",
                DataType = "Text",
                Value =
                    "Software developer interested in building web applications and backend systems.",
                IsRequired = true
            },

            new()
            {
                AttributeDefinitionId = 2,
                Name = "Years of Experience",
                Category = "Professional",
                DataType = "Numeric",
                Value = "1",
                IsRequired = true
            },

            new()
            {
                AttributeDefinitionId = 3,
                Name = "Programming Languages",
                Category = "Skills",
                DataType = "Dropdown",
                Value = "C#, Java, JavaScript, TypeScript",
                IsRequired = false
            },

            new()
            {
                AttributeDefinitionId = 4,
                Name = "Education",
                Category = "Education",
                DataType = "Text",
                Value =
                    "BSc in Computer Science and Engineering",
                IsRequired = true
            },

            new()
            {
                AttributeDefinitionId = 5,
                Name = "Certifications",
                Category = "Professional",
                DataType = "Text",
                Value = "",
                IsRequired = false
            }
        };

        var projects = new List<GeneratedCvProjectViewModel>
        {
            new()
            {
                ProjectId = 1,
                Name = "Employee Management System",
                Period = "Jan 2026 - Apr 2026",
                DescriptionMarkdown =
                    "A role-based employee management application with dashboards, leave management and payroll features.",
                TechnologyTags = new List<string>
                {
                    ".NET",
                    "C#",
                    "Next.js",
                    "TypeScript"
                }
            },

            new()
            {
                ProjectId = 2,
                Name = "Pawtato API",
                Period = "Feb 2026 - Jun 2026",
                DescriptionMarkdown =
                    "A pet digital identity and management API with authentication, pet profiles, QR functionality and notifications.",
                TechnologyTags = new List<string>
                {
                    "NestJS",
                    "TypeScript",
                    "MongoDB",
                    "Docker"
                }
            }
        };

        return new GeneratedCvViewModel
        {
            PositionId = positionId,

            PositionTitle = "Backend Developer",

            CandidateName = "Safar Ahmed",

            Location = "Dhaka, Bangladesh",

            MaxProjects = 3,

            Attributes = attributes,

            Projects = projects
        };
    }
}
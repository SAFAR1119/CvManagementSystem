using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CvManagementSystem.Controllers;

public class ProfileController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var model = BuildDemoProfile();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(CandidateProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Attributes = BuildDemoAttributes();
            model.Projects = BuildDemoProjects();
            model.Cvs = BuildDemoCvs();

            return View("Index", model);
        }

        TempData["Success"] =
            "Your profile changes have been saved for this development session.";

        return RedirectToAction(nameof(Index));
    }

    private static CandidateProfileViewModel BuildDemoProfile()
    {
        return new CandidateProfileViewModel
        {
            FirstName = "Safar",
            LastName = "Ahmed",
            Location = "Dhaka, Bangladesh",
            PhotoUrl = "",

            Attributes = BuildDemoAttributes(),

            Projects = BuildDemoProjects(),

            Cvs = BuildDemoCvs()
        };
    }

    private static List<CandidateAttributeViewModel> BuildDemoAttributes()
    {
        return new List<CandidateAttributeViewModel>
        {
            new()
            {
                Id = 1,
                Name = "Professional Summary",
                Category = "Professional",
                DataType = "Text",
                Value =
                    "Software developer interested in building web applications and backend systems."
            },

            new()
            {
                Id = 2,
                Name = "Years of Experience",
                Category = "Professional",
                DataType = "Numeric",
                Value = "1"
            },

            new()
            {
                Id = 3,
                Name = "Programming Languages",
                Category = "Skills",
                DataType = "Dropdown",
                Value = "C#, Java, JavaScript, TypeScript"
            },

            new()
            {
                Id = 4,
                Name = "Education",
                Category = "Education",
                DataType = "Text",
                Value = "BSc in Computer Science and Engineering"
            }
        };
    }

    private static List<CandidateProjectViewModel> BuildDemoProjects()
    {
        return new List<CandidateProjectViewModel>
        {
            new()
            {
                Id = 1,

                Name = "Employee Management System",

                Period = "2026",

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
                Id = 2,

                Name = "Pawtato API",

                Period = "2026",

                DescriptionMarkdown =
                    "A pet digital identity and management API with authentication, pet profiles, QR functionality and notifications.",

                TechnologyTags = new List<string>
                {
                    "NestJS",
                    "TypeScript",
                    "MongoDB",
                    "Docker"
                }
            },

            new()
            {
                Id = 3,

                Name = "BajarBD Family Mart",

                Period = "2026",

                DescriptionMarkdown =
                    "An online family shopping application built with Spring Boot and Thymeleaf.",

                TechnologyTags = new List<string>
                {
                    "Java",
                    "Spring Boot",
                    "MySQL"
                }
            }
        };
    }

    private static List<CandidateCvSummaryViewModel> BuildDemoCvs()
    {
        return new List<CandidateCvSummaryViewModel>
        {
            new()
            {
                Id = 1,

                PositionTitle = "Backend Developer",

                Title = "Backend Developer CV",

                IsPublished = true,

                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },

            new()
            {
                Id = 2,

                PositionTitle = "Full Stack Developer",

                Title = "Full Stack Developer CV",

                IsPublished = false,

                UpdatedAt = DateTime.UtcNow.AddHours(-5)
            }
        };
    }
}
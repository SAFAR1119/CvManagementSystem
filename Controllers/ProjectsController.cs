using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CvManagementSystem.Controllers;

public class ProjectsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var projects = BuildDemoProjects();

        return View(projects);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new ProjectViewModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };

        LoadTechnologyTags(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ProjectViewModel model)
    {
        LoadTechnologyTags(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["Success"] =
            $"Project '{model.Name}' was prepared successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var model = new ProjectViewModel
        {
            Id = id,
            Name = "Employee Management System",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 4, 1),
            DescriptionMarkdown =
                "A role-based employee management application with dashboards, leave management and payroll features.",

            SelectedTechnologyTagIds = new List<int>
            {
                2,
                5,
                6
            }
        };

        LoadTechnologyTags(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ProjectViewModel model)
    {
        LoadTechnologyTags(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["Success"] =
            $"Project '{model.Name}' was updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var projects = BuildDemoProjects();

        var project = projects.FirstOrDefault(x => x.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    private static void LoadTechnologyTags(ProjectViewModel model)
    {
        model.AvailableTechnologyTags =
            new List<SelectableTechnologyTagViewModel>
            {
                new()
                {
                    Id = 1,
                    Name = "Java"
                },

                new()
                {
                    Id = 2,
                    Name = ".NET"
                },

                new()
                {
                    Id = 3,
                    Name = "C#"
                },

                new()
                {
                    Id = 4,
                    Name = "JavaScript"
                },

                new()
                {
                    Id = 5,
                    Name = "Next.js"
                },

                new()
                {
                    Id = 6,
                    Name = "TypeScript"
                },

                new()
                {
                    Id = 7,
                    Name = "Spring Boot"
                },

                new()
                {
                    Id = 8,
                    Name = "NestJS"
                },

                new()
                {
                    Id = 9,
                    Name = "PostgreSQL"
                },

                new()
                {
                    Id = 10,
                    Name = "MySQL"
                },

                new()
                {
                    Id = 11,
                    Name = "MongoDB"
                },

                new()
                {
                    Id = 12,
                    Name = "Docker"
                },

                new()
                {
                    Id = 13,
                    Name = "React"
                }
            };
    }

    private static List<ProjectViewModel> BuildDemoProjects()
    {
        return new List<ProjectViewModel>
        {
            new()
            {
                Id = 1,
                Name = "Employee Management System",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 4, 1),
                DescriptionMarkdown =
                    "A role-based employee management application with dashboards, leave management and payroll features.",
                SelectedTechnologyTagIds = new List<int>
                {
                    2,
                    3,
                    5,
                    6
                }
            },

            new()
            {
                Id = 2,
                Name = "Pawtato API",
                StartDate = new DateOnly(2026, 2, 1),
                EndDate = new DateOnly(2026, 6, 1),
                DescriptionMarkdown =
                    "A pet digital identity and management API with authentication, pet profiles, QR functionality and notifications.",
                SelectedTechnologyTagIds = new List<int>
                {
                    6,
                    8,
                    11,
                    12
                }
            },

            new()
            {
                Id = 3,
                Name = "BajarBD Family Mart",
                StartDate = new DateOnly(2026, 3, 1),
                EndDate = new DateOnly(2026, 5, 1),
                DescriptionMarkdown =
                    "An online family shopping application built with Spring Boot and Thymeleaf.",
                SelectedTechnologyTagIds = new List<int>
                {
                    1,
                    7,
                    10
                }
            },

            new()
            {
                Id = 4,
                Name = "Personal Portfolio",
                StartDate = new DateOnly(2025, 10, 1),
                DescriptionMarkdown =
                    "A responsive personal developer portfolio showcasing projects, experience and technical skills.",
                SelectedTechnologyTagIds = new List<int>
                {
                    6,
                    5,
                    13
                }
            }
        };
    }
}
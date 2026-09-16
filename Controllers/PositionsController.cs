using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CvManagementSystem.Controllers;

public class PositionsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new PositionViewModel();

        LoadAvailableItems(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PositionViewModel model)
    {
        LoadAvailableItems(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["Success"] =
            $"Position '{model.Title}' was prepared successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var model = new PositionViewModel
        {
            Id = id,

            Title = "Backend Developer",

            Description =
                "Build and maintain backend services, APIs and database integrations.",

            IsPublic = true,

            MaxProjects = 3,

            Version = Guid.NewGuid(),

            SelectedAttributeIds = new List<int>
            {
                1,
                2,
                4
            },

            RequiredAttributeIds = new List<int>
            {
                1,
                2
            },

            SelectedTechnologyTagIds = new List<int>
            {
                1,
                2,
                3
            }
        };

        LoadAvailableItems(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(PositionViewModel model)
    {
        LoadAvailableItems(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["Success"] =
            $"Position '{model.Title}' was updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        return View();
    }

    private static void LoadAvailableItems(PositionViewModel model)
    {
        model.AvailableAttributes =
            new List<SelectableAttributeViewModel>
            {
                new()
                {
                    Id = 1,
                    Name = "First Name",
                    Category = "Personal",
                    DataType = "String"
                },

                new()
                {
                    Id = 2,
                    Name = "Last Name",
                    Category = "Personal",
                    DataType = "String"
                },

                new()
                {
                    Id = 3,
                    Name = "Location",
                    Category = "Personal",
                    DataType = "String"
                },

                new()
                {
                    Id = 4,
                    Name = "Years of Experience",
                    Category = "Professional",
                    DataType = "Numeric"
                },

                new()
                {
                    Id = 5,
                    Name = "Education",
                    Category = "Education",
                    DataType = "Text"
                },

                new()
                {
                    Id = 6,
                    Name = "Professional Summary",
                    Category = "Professional",
                    DataType = "Text"
                },

                new()
                {
                    Id = 7,
                    Name = "Programming Languages",
                    Category = "Skills",
                    DataType = "Dropdown"
                },

                new()
                {
                    Id = 8,
                    Name = "Certifications",
                    Category = "Professional",
                    DataType = "Text"
                }
            };

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
                    Name = "PostgreSQL"
                },

                new()
                {
                    Id = 4,
                    Name = "Docker"
                },

                new()
                {
                    Id = 5,
                    Name = "React"
                },

                new()
                {
                    Id = 6,
                    Name = "Next.js"
                }
            };
    }
}
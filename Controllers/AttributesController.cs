using CvManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace CvManagementSystem.Controllers;

public class AttributesController : Controller
{
    public IActionResult Index()
    {
        var attributes = new List<AttributeDefinition>();

        return View(attributes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CvManagementSystem.ViewModels.AttributeDefinitionViewModel());
    }
}
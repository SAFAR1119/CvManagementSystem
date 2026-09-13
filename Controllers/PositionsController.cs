using Microsoft.AspNetCore.Mvc;

namespace CvManagementSystem.Controllers;

public class PositionsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
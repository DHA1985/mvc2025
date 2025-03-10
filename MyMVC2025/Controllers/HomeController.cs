using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyMVC2025.Models;
using DHA.DataAccess;

namespace MyMVC2025.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        DHA.DataAccess.Math m = new DHA.DataAccess.Math();
        string gr = m.Greeting();
        return View();

    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

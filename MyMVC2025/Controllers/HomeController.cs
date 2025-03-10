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
        //return View();
        //return Content("This is Au testing");
         // Sample customer data
        var customers = new List<Customer>
        {
            new Customer { Id = 1, Name = "Alice Johnson", Email = "alice@example.com" },
            new Customer { Id = 2, Name = "Bob Smith", Email = "bob@example.com" },
            new Customer { Id = 3, Name = "Charlie Davis", Email = "charlie@example.com" }
        };

        // Pass data to the View
        return View(customers);
       

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

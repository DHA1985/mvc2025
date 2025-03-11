using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyMVC2025.Models;
using MyMVC.DataAccess;
using MyMVC.Model;
namespace MyMVC2025.Controllers;

public class AzureCertsController : Controller
{
    private readonly ILogger<AzureCertsController> _logger;

    public AzureCertsController(ILogger<AzureCertsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        
        return View();
       

    }

    public IActionResult Details(string id)
    {
        //ViewData["CertificationId"] = id;
        //return View();
       // MyMVC.Model.Customer cu = new MyMVC.Model.Customer( 3, "APM", "APM@gmail.com");
        //ViewData["Customer"] = cu;
        return Content(id);
        
        //return View();
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

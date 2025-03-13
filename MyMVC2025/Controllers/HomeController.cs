using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyMVC2025.Models;
using MyMVC.Model;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
        
              
        //return View(c);
        //return View();
        //return Content("This is Au testing");
         // Sample customer data
        Customer cu = new Customer(1, "Au", "test@emai.com");
        Customer cu2 = new Customer(1, "Au2", "test@emai.com");
        var customers = new List<Customer>();
        customers.Add(new Customer(1,"Au","test@gmail.com"));
        customers.Add(new Customer(2, "Duc", "Duc@gmail.com"));
        
       /*  var customers = new List<Customer>;
       
        {
            new Customer { Id = 1, Name = "Alice Johnson", Email = "alice@example.com" },
            new Customer { Id = 2, Name = "Bob Smith", Email = "bob@example.com" },
            new Customer { Id = 3, Name = "Charlie Davis", Email = "charlie@example.com" }
        };  */

        // Pass data to the View
        return View(customers);
        //Au test return json from API
        

    }

    //index method to return data from API
      


    public async Task<IActionResult> WeatherForecast()
    {
       
                using (HttpClient httpClient = new HttpClient()) // Initialize HttpClient here
        {
            var response = await httpClient.GetStringAsync("http://localhost:5000/WeatherForecast");
            var formattedJson = JsonSerializer.Serialize(
            JsonSerializer.Deserialize<object>(response), 
            new JsonSerializerOptions { WriteIndented = true }
        );


        return Content(formattedJson);
           
        }
        //return View(products);
       
    }
    
    public string GetAPIData()
    {
        

        return "";

    }
   

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyMVC2025.Models;
using MyMVC.Model;
using System.Data;
using DataAccess; //My own namespace for database acceess
namespace MyMVC2025.Controllers;

public class TestMysqlController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public TestMysqlController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        
          string connectionString = "Server=your-server-name.mysql.database.azure.com;Port=3306;Database=your-database-name;User ID=your-username@your-server-name;Password=your-password;SslMode=Preferred;";

            // Initialize MySqlConnect
            MySqlConnect mysql = new MySqlConnect(connectionString);

            // Example: SELECT query
            string selectQuery = "SELECT * FROM Users";
            DataTable users = mysql.ExecuteSelectQuery(selectQuery);

            foreach (DataRow row in users.Rows)
            {
                Console.WriteLine($"User: {row["UserName"]}, Email: {row["Email"]}");
            }

            // Example: INSERT query
            string insertQuery = "INSERT INTO Users (UserName, Email) VALUES ('JohnDoe', 'johndoe@example.com')";
            int rowsAffected = mysql.ExecuteNonQuery(insertQuery);
            Console.WriteLine($"Rows affected: {rowsAffected}");

            // Example: Execute a scalar query (e.g., getting the total number of users)
            string scalarQuery = "SELECT COUNT(*) FROM Users";
            object totalUsers = mysql.ExecuteScalar(scalarQuery);
            Console.WriteLine($"Total number of users: {totalUsers}");     
       
           return View(users);
       
    }

   

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

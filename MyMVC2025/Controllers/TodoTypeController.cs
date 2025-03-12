using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyMVC2025.Models;
using MyMVC.Model;
using DataAccess;
using System.Data;
namespace MyMVC2025.Controllers;
public class TodoTypeController : Controller
{
    public const string INSERT = "todotype_Insert";
    public const string UPDATE = "todotype_Update";
    public const string DELETE = "todotype_Delete";
    public const string GET_ALL = "todotype_GetAll";
    public const string GET_BY_ID = "todotype_GetByID";

    private readonly SqlHelper _sqlHelper;

    public TodoTypeController(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public IActionResult GetAll()
    {
        
        return View();
    }

}
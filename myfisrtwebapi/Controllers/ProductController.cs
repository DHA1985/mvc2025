using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

[Route("api/[controller]")] //this will return with url concat /api/product
//[Route("/")] //this will run at root url
[ApiController]
public class ProductController : ControllerBase
{
    [HttpGet]
    public IActionResult GetProducts()
    {
        var products = new List<string> { "Laptop", "Phone", "Tablet" };
        return Ok(products);
    }
}
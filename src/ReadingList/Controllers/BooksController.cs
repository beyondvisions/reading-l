using Microsoft.AspNetCore.Mvc;

namespace ReadingList.Controllers;

[ApiController]
[Route("books")]
public class BooksController : ControllerBase
{
    [HttpPost]
    public IActionResult Create()
    {
        return Ok("BooksController is alive");
    }
}
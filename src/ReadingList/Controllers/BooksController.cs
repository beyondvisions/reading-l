using Microsoft.AspNetCore.Mvc;
using ReadingList.Contracts;
using ReadingList.Services;

namespace ReadingList.Controllers;

[ApiController]
[Route("books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(BookRequest request, CancellationToken ct)
    {
        var book = await _bookService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Create), new { id = book.Id }, book);
    }
}
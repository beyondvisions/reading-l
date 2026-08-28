using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ReadingList.Contracts;
using ReadingList.Services;

namespace ReadingList.Controllers;

[ApiController]
[Route("books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IValidator<BookRequest> _validator;

    public BooksController(IBookService bookService, IValidator<BookRequest> validator)
    {
        _bookService = bookService;
        _validator = validator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(BookRequest request, CancellationToken ct)
    {
        var result = await _validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

            return ValidationProblem(ModelState);
        }

        var book = await _bookService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Create), new { id = book.Id }, book);
    }
}
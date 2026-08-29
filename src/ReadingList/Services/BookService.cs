using ReadingList.Contracts;

namespace ReadingList.Services;

public class BookService : IBookService
{
    private readonly ILogger<BookService> _logger;

    public BookService(ILogger<BookService> logger)
    {
        _logger = logger;
    }

    public Task<BookResponse> CreateAsync(BookRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating book with payload: {@Request}", request);

        var response = new BookResponse(
            Guid.NewGuid(),
            request.Title,
            request.Author,
            request.PageCount,
            request.Status,
            DateTime.UtcNow);

        _logger.LogInformation("Book created: {@Response}", response);

        return Task.FromResult(response);
    }
}
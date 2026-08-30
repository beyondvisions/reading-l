using ReadingList.Contracts;
using ReadingList.Entities;
using ReadingList.Repositories;

namespace ReadingList.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _repository;
    private readonly ILogger<BookService> _logger;

    public BookService(IBookRepository repository, ILogger<BookService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BookResponse> CreateAsync(BookRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating book with payload: {@Request}", request);

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Author = request.Author,
            PageCount = request.PageCount,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(book, ct);

        var response = new BookResponse(
            created.Id,
            created.Title,
            created.Author,
            created.PageCount,
            created.Status,
            created.CreatedAt);

        _logger.LogInformation("Book persisted: {BookId}", created.Id);

        return response;
    }
}
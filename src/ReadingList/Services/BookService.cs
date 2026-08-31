using ReadingList.Contracts;
using ReadingList.Exceptions;
using ReadingList.Mappings;
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

        if (await _repository.ExistsAsync(request.Title, request.Author, ct))
        {
            throw new ConflictException(
                $"A book titled '{request.Title}' by {request.Author} already exists.");
        }

        var created = await _repository.AddAsync(request.ToEntity(), ct);

        _logger.LogInformation("Book persisted: {BookId}", created.Id);

        return created.ToResponse();
    }

    public async Task<BookResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var book = await _repository.GetByIdAsync(id, ct);

        if (book is null)
        {
            throw new NotFoundException($"Book with id {id} was not found.");
        }

        return book.ToResponse();
    }
}
using ReadingList.Contracts;
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

        var created = await _repository.AddAsync(request.ToEntity(), ct);

        _logger.LogInformation("Book persisted: {BookId}", created.Id);

        return created.ToResponse();
    }
}
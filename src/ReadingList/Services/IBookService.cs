using ReadingList.Contracts;

namespace ReadingList.Services;

public interface IBookService
{
    Task<BookResponse> CreateAsync(BookRequest request, CancellationToken ct = default);
}
using ReadingList.Entities;

namespace ReadingList.Repositories;

public interface IBookRepository
{
    Task<Book> AddAsync(Book book, CancellationToken ct = default);
}
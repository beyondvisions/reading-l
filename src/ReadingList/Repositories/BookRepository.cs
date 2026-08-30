using ReadingList.Data;
using ReadingList.Entities;

namespace ReadingList.Repositories;

public class BookRepository : IBookRepository
{
    private readonly ReadingListDbContext _context;

    public BookRepository(ReadingListDbContext context)
    {
        _context = context;
    }

    public async Task<Book> AddAsync(Book book, CancellationToken ct = default)
    {
        await _context.Books.AddAsync(book, ct);
        await _context.SaveChangesAsync(ct);
        return book;
    }
}
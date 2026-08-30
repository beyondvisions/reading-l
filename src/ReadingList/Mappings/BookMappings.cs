using ReadingList.Contracts;
using ReadingList.Entities;

namespace ReadingList.Mappings;

public static class BookMappings
{
    public static Book ToEntity(this BookRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Author = request.Author,
        PageCount = request.PageCount,
        Status = request.Status,
        CreatedAt = DateTime.UtcNow
    };

    public static BookResponse ToResponse(this Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.PageCount,
        book.Status,
        book.CreatedAt);
}
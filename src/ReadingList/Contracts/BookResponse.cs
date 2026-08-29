using ReadingList.Enums;

namespace ReadingList.Contracts;

public record BookResponse(
    Guid Id,
    string Title,
    string Author,
    int PageCount,
    BookStatus Status,
    DateTime CreatedAt);
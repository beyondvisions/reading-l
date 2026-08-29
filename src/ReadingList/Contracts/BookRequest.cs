using ReadingList.Enums;

namespace ReadingList.Contracts;

public record BookRequest(
    string Title,
    string Author,
    int PageCount,
    BookStatus Status);
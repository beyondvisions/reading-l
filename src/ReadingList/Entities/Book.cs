using ReadingList.Enums;

namespace ReadingList.Entities;

public class Book
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public int PageCount { get; set; }
    public BookStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
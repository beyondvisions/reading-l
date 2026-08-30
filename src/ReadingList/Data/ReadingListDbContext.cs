using Microsoft.EntityFrameworkCore;
using ReadingList.Configurations;
using ReadingList.Entities;

namespace ReadingList.Data;

public class ReadingListDbContext : DbContext
{
    public ReadingListDbContext(DbContextOptions<ReadingListDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BookConfiguration());
    }
}
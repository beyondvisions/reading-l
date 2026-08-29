using Microsoft.EntityFrameworkCore;
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
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(b => b.Author)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(b => b.CreatedAt)
                .IsRequired();
        });
    }
}
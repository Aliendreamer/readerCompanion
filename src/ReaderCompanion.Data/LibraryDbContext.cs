using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Organising;

namespace ReaderCompanion.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Genre> Genres => Set<Genre>();

    public DbSet<Collection> Collections => Set<Collection>();

    public DbSet<BookFile> BookFiles => Set<BookFile>();

    public DbSet<LibraryFolder> LibraryFolders => Set<LibraryFolder>();

    public DbSet<DismissedDuplicate> DismissedDuplicates => Set<DismissedDuplicate>();

    public DbSet<ReadingSession> ReadingSessions => Set<ReadingSession>();

    public DbSet<FileOperation> FileOperations => Set<FileOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(book =>
        {
            book.HasOne(b => b.Series).WithMany().HasForeignKey(b => b.SeriesId).OnDelete(DeleteBehavior.SetNull);
            book.Property(b => b.SeriesIndex).HasConversion<double?>();
            book.Property(b => b.ReadStatus).HasConversion<string>();
            book.HasMany(b => b.Sessions).WithOne().HasForeignKey(s => s.BookId);
        });

        modelBuilder.Entity<Author>().HasIndex(a => a.NormalizedName).IsUnique();
        modelBuilder.Entity<BookAuthor>(link =>
        {
            link.HasKey(l => new { l.BookId, l.AuthorId });
            link.HasOne(l => l.Book).WithMany(b => b.Authors).HasForeignKey(l => l.BookId);
            link.HasOne(l => l.Author).WithMany().HasForeignKey(l => l.AuthorId);
        });

        modelBuilder.Entity<Genre>().HasIndex(g => g.NormalizedName).IsUnique();
        modelBuilder.Entity<BookGenre>(link =>
        {
            link.HasKey(l => new { l.BookId, l.GenreId });
            link.HasOne(l => l.Book).WithMany(b => b.Genres).HasForeignKey(l => l.BookId);
            link.HasOne(l => l.Genre).WithMany().HasForeignKey(l => l.GenreId);
        });

        modelBuilder.Entity<BookCollection>(link =>
        {
            link.HasKey(l => new { l.BookId, l.CollectionId });
            link.HasOne(l => l.Book).WithMany(b => b.Collections).HasForeignKey(l => l.BookId);
            link.HasOne(l => l.Collection).WithMany().HasForeignKey(l => l.CollectionId);
        });

        modelBuilder.Entity<Collection>(collection =>
        {
            collection.HasIndex(c => new { c.Kind, c.NormalizedName }).IsUnique();
            collection.Property(c => c.Kind).HasConversion<string>();
        });

        modelBuilder.Entity<BookFile>(file =>
        {
            file.HasIndex(f => f.Path).IsUnique();
            file.HasIndex(f => f.ContentHash);
            file.HasOne(f => f.Book).WithMany(b => b.Files).HasForeignKey(f => f.BookId);
            file.Property(f => f.Format).HasConversion<string>();
            file.Property(f => f.Status).HasConversion<string>();
        });

        modelBuilder.Entity<LibraryFolder>().HasIndex(f => f.Path).IsUnique();

        modelBuilder.Entity<FileOperation>()
            .HasMany(o => o.Moves).WithOne().HasForeignKey(m => m.OperationId);

        // Removing either book removes the dismissal.
        modelBuilder.Entity<DismissedDuplicate>(pair =>
        {
            pair.HasKey(p => new { p.BookIdA, p.BookIdB });
            pair.HasOne<Book>().WithMany().HasForeignKey(p => p.BookIdA).OnDelete(DeleteBehavior.Cascade);
            pair.HasOne<Book>().WithMany().HasForeignKey(p => p.BookIdB).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

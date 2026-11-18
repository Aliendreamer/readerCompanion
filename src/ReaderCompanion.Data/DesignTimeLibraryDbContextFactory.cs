using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReaderCompanion.Data;

// Used only by `dotnet ef` tooling to build the model; the app never uses this path.
internal sealed class DesignTimeLibraryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
{
    public LibraryDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite("Data Source=design.db").Options);
}

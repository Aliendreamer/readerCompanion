using Microsoft.EntityFrameworkCore;

namespace ReaderCompanion.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options);

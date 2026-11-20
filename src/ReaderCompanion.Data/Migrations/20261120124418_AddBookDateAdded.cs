using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReaderCompanion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookDateAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateAddedUtc",
                table: "Books",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Books from before this column: their oldest file's last-modified time is the best guess at when
            // they were added; a book with no files gets the time of this migration.
            migrationBuilder.Sql("""
                UPDATE Books SET DateAddedUtc = COALESCE(
                    (SELECT MIN(f.LastWriteUtc) FROM BookFiles f WHERE f.BookId = Books.Id),
                    strftime('%Y-%m-%d %H:%M:%S', 'now'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateAddedUtc",
                table: "Books");
        }
    }
}

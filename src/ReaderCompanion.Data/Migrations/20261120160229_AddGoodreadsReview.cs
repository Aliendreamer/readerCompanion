using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReaderCompanion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodreadsReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoodreadsPending",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GoodreadsId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Authors = table.Column<string>(type: "TEXT", nullable: false),
                    Isbn = table.Column<string>(type: "TEXT", nullable: true),
                    Rating = table.Column<int>(type: "INTEGER", nullable: true),
                    DateRead = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Shelves = table.Column<string>(type: "TEXT", nullable: false),
                    Series = table.Column<string>(type: "TEXT", nullable: true),
                    SeriesIndex = table.Column<double>(type: "REAL", nullable: true),
                    BookId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodreadsPending", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodreadsPending_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodreadsPending_BookId",
                table: "GoodreadsPending",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodreadsPending_GoodreadsId",
                table: "GoodreadsPending",
                column: "GoodreadsId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodreadsPending");
        }
    }
}

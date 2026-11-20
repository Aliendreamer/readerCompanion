using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReaderCompanion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDismissedDuplicates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DismissedDuplicates",
                columns: table => new
                {
                    BookIdA = table.Column<int>(type: "INTEGER", nullable: false),
                    BookIdB = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DismissedDuplicates", x => new { x.BookIdA, x.BookIdB });
                    table.ForeignKey(
                        name: "FK_DismissedDuplicates_Books_BookIdA",
                        column: x => x.BookIdA,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DismissedDuplicates_Books_BookIdB",
                        column: x => x.BookIdB,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DismissedDuplicates_BookIdB",
                table: "DismissedDuplicates",
                column: "BookIdB");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DismissedDuplicates");
        }
    }
}

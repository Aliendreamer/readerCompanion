using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReaderCompanion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodreadsImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FromGoodreads",
                table: "ReadingSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GoodreadsId",
                table: "Books",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GoodreadsRating",
                table: "Books",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoodreadsStatus",
                table: "Books",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_GoodreadsId",
                table: "Books",
                column: "GoodreadsId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_GoodreadsId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "FromGoodreads",
                table: "ReadingSessions");

            migrationBuilder.DropColumn(
                name: "GoodreadsId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "GoodreadsRating",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "GoodreadsStatus",
                table: "Books");
        }
    }
}

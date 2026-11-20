using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReaderCompanion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Root = table.Column<string>(type: "TEXT", nullable: false),
                    UndoneUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileMove",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperationId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    From = table.Column<string>(type: "TEXT", nullable: false),
                    To = table.Column<string>(type: "TEXT", nullable: false),
                    Done = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileMove", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileMove_FileOperations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "FileOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileMove_OperationId",
                table: "FileMove",
                column: "OperationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileMove");

            migrationBuilder.DropTable(
                name: "FileOperations");
        }
    }
}

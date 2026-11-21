using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReaderCompanion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDrive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriveFileId",
                table: "BookFiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriveSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DriveId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    AddedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriveSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookFiles_DriveFileId",
                table: "BookFiles",
                column: "DriveFileId");

            migrationBuilder.CreateIndex(
                name: "IX_DriveSources_DriveId",
                table: "DriveSources",
                column: "DriveId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriveSources");

            migrationBuilder.DropIndex(
                name: "IX_BookFiles_DriveFileId",
                table: "BookFiles");

            migrationBuilder.DropColumn(
                name: "DriveFileId",
                table: "BookFiles");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Clipboard.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClipboardTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClipboardTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClipboardItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Preview = table.Column<string>(type: "TEXT", maxLength: 600, nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    ClipboardTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Format = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    BinaryData = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClipboardItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClipboardItems_ClipboardTypes_ClipboardTypeId",
                        column: x => x.ClipboardTypeId,
                        principalTable: "ClipboardTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ClipboardTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Plain text content", "Text" },
                    { 2, "Image content", "Image" },
                    { 3, "File paths and file content", "File" },
                    { 4, "Color values", "Color" },
                    { 5, "Source code snippets", "Code" },
                    { 6, "Url to a webpage", "Link" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClipboardItems_ClipboardTypeId",
                table: "ClipboardItems",
                column: "ClipboardTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClipboardItems");

            migrationBuilder.DropTable(
                name: "ClipboardTypes");
        }
    }
}

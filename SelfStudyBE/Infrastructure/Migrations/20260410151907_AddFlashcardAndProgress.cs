using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashcardAndProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlashcardProgresses_FlashcardId",
                table: "FlashcardProgresses");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "Flashcards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "FlashcardProgresses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_FlashcardProgresses_FlashcardId_UserId",
                table: "FlashcardProgresses",
                columns: new[] { "FlashcardId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlashcardProgresses_FlashcardId_UserId",
                table: "FlashcardProgresses");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Flashcards");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "FlashcardProgresses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_FlashcardProgresses_FlashcardId",
                table: "FlashcardProgresses",
                column: "FlashcardId");
        }
    }
}

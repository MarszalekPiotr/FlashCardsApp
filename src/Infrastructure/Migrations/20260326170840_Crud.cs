using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

    /// <inheritdoc />
    public partial class Crud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flashcards_LanguageAccounts_LanguageAccountId",
                schema: "dbo",
                table: "Flashcards");

            migrationBuilder.RenameColumn(
                name: "LanguageAccountId",
                schema: "dbo",
                table: "Flashcards",
                newName: "FlashcardCollectionId");

            migrationBuilder.RenameIndex(
                name: "IX_Flashcards_LanguageAccountId",
                schema: "dbo",
                table: "Flashcards",
                newName: "IX_Flashcards_FlashcardCollectionId");

            migrationBuilder.CreateTable(
                name: "FlashcardCollections",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashcardCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashcardCollections_LanguageAccounts_LanguageAccountId",
                        column: x => x.LanguageAccountId,
                        principalSchema: "dbo",
                        principalTable: "LanguageAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlashcardCollections_LanguageAccountId",
                schema: "dbo",
                table: "FlashcardCollections",
                column: "LanguageAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Flashcards_FlashcardCollections_FlashcardCollectionId",
                schema: "dbo",
                table: "Flashcards",
                column: "FlashcardCollectionId",
                principalSchema: "dbo",
                principalTable: "FlashcardCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flashcards_FlashcardCollections_FlashcardCollectionId",
                schema: "dbo",
                table: "Flashcards");

            migrationBuilder.DropTable(
                name: "FlashcardCollections",
                schema: "dbo");

            migrationBuilder.RenameColumn(
                name: "FlashcardCollectionId",
                schema: "dbo",
                table: "Flashcards",
                newName: "LanguageAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Flashcards_FlashcardCollectionId",
                schema: "dbo",
                table: "Flashcards",
                newName: "IX_Flashcards_LanguageAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Flashcards_LanguageAccounts_LanguageAccountId",
                schema: "dbo",
                table: "Flashcards",
                column: "LanguageAccountId",
                principalSchema: "dbo",
                principalTable: "LanguageAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }

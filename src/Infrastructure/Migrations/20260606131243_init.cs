using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        private static readonly string[] columns = new[] { "Id", "Code", "CreatedAt", "IsActive", "Name", "UpdatedAt" };
        private static readonly string[] columnsArray = new[] { "ProcessedOnUtc", "RetryCount" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Language",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Language", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessageConsumers",
                schema: "dbo",
                columns: table => new
                {
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandlerType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessageConsumers", x => new { x.OutboxMessageId, x.HandlerType });
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LanguageAccounts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProficiencyLevel = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LanguageAccounts_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "dbo",
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LanguageAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlashcardCollections",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Flashcards",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlashcardCollectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentenceWithBlanks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Translation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Synonyms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flashcards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flashcards_FlashcardCollections_FlashcardCollectionId",
                        column: x => x.FlashcardCollectionId,
                        principalSchema: "dbo",
                        principalTable: "FlashcardCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlashcardReviews",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlashcardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewResult = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashcardReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashcardReviews_Flashcards_FlashcardId",
                        column: x => x.FlashcardId,
                        principalSchema: "dbo",
                        principalTable: "Flashcards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SrsStates",
                schema: "dbo",
                columns: table => new
                {
                    FlashcardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    EaseFactor = table.Column<double>(type: "float", nullable: false),
                    Repetitions = table.Column<int>(type: "int", nullable: false),
                    NextReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SrsStates", x => x.FlashcardId);
                    table.ForeignKey(
                        name: "FK_SrsStates_Flashcards_FlashcardId",
                        column: x => x.FlashcardId,
                        principalSchema: "dbo",
                        principalTable: "Flashcards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Language",
                columns: columns,
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "EN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "English", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "ES", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Spanish", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "FR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "French", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "DE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "German", null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "ZH", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Chinese", null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "JA", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Japanese", null },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "RU", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Russian", null },
                    { new Guid("88888888-8888-8888-8888-888888888888"), "PL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Polish", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlashcardCollections_LanguageAccountId",
                schema: "dbo",
                table: "FlashcardCollections",
                column: "LanguageAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashcardReviews_FlashcardId",
                schema: "dbo",
                table: "FlashcardReviews",
                column: "FlashcardId");

            migrationBuilder.CreateIndex(
                name: "IX_Flashcards_FlashcardCollectionId",
                schema: "dbo",
                table: "Flashcards",
                column: "FlashcardCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageAccounts_LanguageId",
                schema: "dbo",
                table: "LanguageAccounts",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageAccounts_UserId",
                schema: "dbo",
                table: "LanguageAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessageConsumers_OutboxMessageId",
                schema: "dbo",
                table: "OutboxMessageConsumers",
                column: "OutboxMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_RetryCount",
                schema: "dbo",
                table: "OutboxMessages",
                columns: columnsArray);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "dbo",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlashcardReviews",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OutboxMessageConsumers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SrsStates",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Flashcards",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FlashcardCollections",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "LanguageAccounts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Language",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");
        }
    }
}

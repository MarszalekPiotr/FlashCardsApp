using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class languages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LanguageAccounts_Language_LanguageId",
                schema: "dbo",
                table: "LanguageAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Language",
                schema: "dbo",
                table: "Language");

            migrationBuilder.RenameTable(
                name: "Language",
                schema: "dbo",
                newName: "Languages",
                newSchema: "dbo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Languages",
                schema: "dbo",
                table: "Languages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LanguageAccounts_Languages_LanguageId",
                schema: "dbo",
                table: "LanguageAccounts",
                column: "LanguageId",
                principalSchema: "dbo",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LanguageAccounts_Languages_LanguageId",
                schema: "dbo",
                table: "LanguageAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Languages",
                schema: "dbo",
                table: "Languages");

            migrationBuilder.RenameTable(
                name: "Languages",
                schema: "dbo",
                newName: "Language",
                newSchema: "dbo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Language",
                schema: "dbo",
                table: "Language",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LanguageAccounts_Language_LanguageId",
                schema: "dbo",
                table: "LanguageAccounts",
                column: "LanguageId",
                principalSchema: "dbo",
                principalTable: "Language",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

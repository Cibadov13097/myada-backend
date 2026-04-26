using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostFoundService.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(LostFoundDbContext))]
    [Migration("20260426103000_LostFoundItemUseCategoryId")]
    public partial class LostFoundItemUseCategoryId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                schema: "lostfound",
                table: "LostFoundItems",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO lostfound."Categories" ("Name", "CreatedAt", "IsDeleted")
                SELECT DISTINCT
                    COALESCE(NULLIF(BTRIM(item."Category"), ''), 'Other') AS "Name",
                    NOW(),
                    FALSE
                FROM lostfound."LostFoundItems" AS item
                LEFT JOIN lostfound."Categories" AS category
                    ON LOWER(BTRIM(category."Name")) = LOWER(COALESCE(NULLIF(BTRIM(item."Category"), ''), 'Other'))
                WHERE category."Id" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE lostfound."LostFoundItems" AS item
                SET "CategoryId" = category."Id"
                FROM lostfound."Categories" AS category
                WHERE LOWER(BTRIM(category."Name")) = LOWER(COALESCE(NULLIF(BTRIM(item."Category"), ''), 'Other'));
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                schema: "lostfound",
                table: "LostFoundItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LostFoundItems_CategoryId",
                schema: "lostfound",
                table: "LostFoundItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_LostFoundItems_Categories_CategoryId",
                schema: "lostfound",
                table: "LostFoundItems",
                column: "CategoryId",
                principalSchema: "lostfound",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "lostfound",
                table: "LostFoundItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "lostfound",
                table: "LostFoundItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE lostfound."LostFoundItems" AS item
                SET "Category" = category."Name"
                FROM lostfound."Categories" AS category
                WHERE item."CategoryId" = category."Id";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_LostFoundItems_Categories_CategoryId",
                schema: "lostfound",
                table: "LostFoundItems");

            migrationBuilder.DropIndex(
                name: "IX_LostFoundItems_CategoryId",
                schema: "lostfound",
                table: "LostFoundItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "lostfound",
                table: "LostFoundItems");
        }
    }
}

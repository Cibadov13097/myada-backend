using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LostFoundService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class category_table_added : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "LostItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "FoundItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LostItems_CategoryId",
                table: "LostItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItems_CategoryId",
                table: "FoundItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_FoundItems_Categories_CategoryId",
                table: "FoundItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LostItems_Categories_CategoryId",
                table: "LostItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoundItems_Categories_CategoryId",
                table: "FoundItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LostItems_Categories_CategoryId",
                table: "LostItems");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_LostItems_CategoryId",
                table: "LostItems");

            migrationBuilder.DropIndex(
                name: "IX_FoundItems_CategoryId",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "LostItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "FoundItems");
        }
    }
}

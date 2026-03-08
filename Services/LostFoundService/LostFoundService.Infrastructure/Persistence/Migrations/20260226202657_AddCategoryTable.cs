using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LostFoundService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "lostfound",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO lostfound.\"Categories\" (\"Name\") VALUES ('Other');");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                schema: "lostfound",
                table: "LostItems",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                schema: "lostfound",
                table: "FoundItems",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_LostItems_CategoryId",
                schema: "lostfound",
                table: "LostItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItems_CategoryId",
                schema: "lostfound",
                table: "FoundItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_FoundItems_Categories_CategoryId",
                schema: "lostfound",
                table: "FoundItems",
                column: "CategoryId",
                principalSchema: "lostfound",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LostItems_Categories_CategoryId",
                schema: "lostfound",
                table: "LostItems",
                column: "CategoryId",
                principalSchema: "lostfound",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoundItems_Categories_CategoryId",
                schema: "lostfound",
                table: "FoundItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LostItems_Categories_CategoryId",
                schema: "lostfound",
                table: "LostItems");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "lostfound");

            migrationBuilder.DropIndex(
                name: "IX_LostItems_CategoryId",
                schema: "lostfound",
                table: "LostItems");

            migrationBuilder.DropIndex(
                name: "IX_FoundItems_CategoryId",
                schema: "lostfound",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "lostfound",
                table: "LostItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "lostfound",
                table: "FoundItems");
        }
    }
}

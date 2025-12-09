using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostFoundService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class lostItem_column_added : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationFound",
                table: "LostItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationFound",
                table: "LostItems");
        }
    }
}

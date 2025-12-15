using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostFoundService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class locationlost_column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LocationFound",
                table: "LostItems",
                newName: "LocationLost");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LocationLost",
                table: "LostItems",
                newName: "LocationFound");
        }
    }
}

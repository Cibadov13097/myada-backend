using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocationService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class column_capacity_added : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                schema: "location",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "location",
                table: "Buildings");

            migrationBuilder.RenameColumn(
                name: "Code",
                schema: "location",
                table: "Rooms",
                newName: "Number");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                schema: "location",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity",
                schema: "location",
                table: "Rooms");

            migrationBuilder.RenameColumn(
                name: "Number",
                schema: "location",
                table: "Rooms",
                newName: "Code");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "location",
                table: "Buildings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "location",
                table: "Buildings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}

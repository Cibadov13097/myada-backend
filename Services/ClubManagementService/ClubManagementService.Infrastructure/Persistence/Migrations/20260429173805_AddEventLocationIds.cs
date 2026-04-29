using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLocationIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuildingId",
                schema: "club",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                schema: "club",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuildingId",
                schema: "club",
                table: "EventProposals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                schema: "club",
                table: "EventProposals",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingId",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RoomId",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                schema: "club",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "RoomId",
                schema: "club",
                table: "EventProposals");
        }
    }
}

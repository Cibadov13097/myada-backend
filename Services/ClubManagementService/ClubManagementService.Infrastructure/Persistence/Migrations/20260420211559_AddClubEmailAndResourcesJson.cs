using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClubEmailAndResourcesJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "club",
                table: "Clubs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourcesJson",
                schema: "club",
                table: "Clubs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "club",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ResourcesJson",
                schema: "club",
                table: "Clubs");
        }
    }
}

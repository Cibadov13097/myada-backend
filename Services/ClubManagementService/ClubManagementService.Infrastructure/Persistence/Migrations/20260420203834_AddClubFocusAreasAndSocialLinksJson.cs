using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClubFocusAreasAndSocialLinksJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FocusAreasJson",
                schema: "club",
                table: "Clubs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialLinksJson",
                schema: "club",
                table: "Clubs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FocusAreasJson",
                schema: "club",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "SocialLinksJson",
                schema: "club",
                table: "Clubs");
        }
    }
}

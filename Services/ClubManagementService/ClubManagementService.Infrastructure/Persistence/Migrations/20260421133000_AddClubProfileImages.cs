using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations;

public partial class AddClubProfileImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProfileImageUrl",
            schema: "club",
            table: "Clubs",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProposedProfileImageUrl",
            schema: "club",
            table: "Clubs",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ProfileImageUrl",
            schema: "club",
            table: "Clubs");

        migrationBuilder.DropColumn(
            name: "ProposedProfileImageUrl",
            schema: "club",
            table: "Clubs");
    }
}


using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations;

public partial class AddImageUrlToEventProposals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ImageUrl",
            schema: "club",
            table: "EventProposals",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ImageUrl",
            schema: "club",
            table: "EventProposals");
    }
}

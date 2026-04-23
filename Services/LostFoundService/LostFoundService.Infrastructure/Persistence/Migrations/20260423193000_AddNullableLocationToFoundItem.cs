using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostFoundService.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(LostFoundDbContext))]
    [Migration("20260423193000_AddNullableLocationToFoundItem")]
    public partial class AddNullableLocationToFoundItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                schema: "lostfound",
                table: "FoundItems",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                schema: "lostfound",
                table: "FoundItems");
        }
    }
}

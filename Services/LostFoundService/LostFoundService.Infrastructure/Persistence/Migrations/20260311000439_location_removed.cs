using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LostFoundService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class location_removed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoundItems_Locations_LocationId",
                schema: "lostfound",
                table: "FoundItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LostItems_Locations_LocationId",
                schema: "lostfound",
                table: "LostItems");

            migrationBuilder.DropTable(
                name: "Locations",
                schema: "lostfound");

            migrationBuilder.DropIndex(
                name: "IX_LostItems_LocationId",
                schema: "lostfound",
                table: "LostItems");

            migrationBuilder.DropIndex(
                name: "IX_FoundItems_LocationId",
                schema: "lostfound",
                table: "FoundItems");

            migrationBuilder.RenameColumn(
                name: "LocationId",
                schema: "lostfound",
                table: "LostItems",
                newName: "RoomId");

            migrationBuilder.RenameColumn(
                name: "LocationId",
                schema: "lostfound",
                table: "FoundItems",
                newName: "RoomId");

            migrationBuilder.AddColumn<int>(
                name: "BuildingId",
                schema: "lostfound",
                table: "LostItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuildingId",
                schema: "lostfound",
                table: "FoundItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingId",
                schema: "lostfound",
                table: "LostItems");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                schema: "lostfound",
                table: "FoundItems");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                schema: "lostfound",
                table: "LostItems",
                newName: "LocationId");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                schema: "lostfound",
                table: "FoundItems",
                newName: "LocationId");

            migrationBuilder.CreateTable(
                name: "Locations",
                schema: "lostfound",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "text", nullable: true),
                    Floor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "text", nullable: true),
                    Place = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Room = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LostItems_LocationId",
                schema: "lostfound",
                table: "LostItems",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItems_LocationId",
                schema: "lostfound",
                table: "FoundItems",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FoundItems_Locations_LocationId",
                schema: "lostfound",
                table: "FoundItems",
                column: "LocationId",
                principalSchema: "lostfound",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LostItems_Locations_LocationId",
                schema: "lostfound",
                table: "LostItems",
                column: "LocationId",
                principalSchema: "lostfound",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

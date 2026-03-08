using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostFoundService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class categorytablemodified_mig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "lostfound",
                table: "Categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                schema: "lostfound",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "lostfound",
                table: "Categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                schema: "lostfound",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "lostfound",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                schema: "lostfound",
                table: "Categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                schema: "lostfound",
                table: "Categories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "lostfound",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "lostfound",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "lostfound",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "lostfound",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "lostfound",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                schema: "lostfound",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                schema: "lostfound",
                table: "Categories");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V3_EventTicketsAndCheckin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "club",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                schema: "club",
                table: "EventRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CheckedInBy",
                schema: "club",
                table: "EventRegistrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventEndTimeSnapshot",
                schema: "club",
                table: "EventRegistrations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EventImageUrlSnapshot",
                schema: "club",
                table: "EventRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventLocationSnapshot",
                schema: "club",
                table: "EventRegistrations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventNameSnapshot",
                schema: "club",
                table: "EventRegistrations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EventSeatLimitSnapshot",
                schema: "club",
                table: "EventRegistrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventStartTimeSnapshot",
                schema: "club",
                table: "EventRegistrations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsCheckedIn",
                schema: "club",
                table: "EventRegistrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastScannedAt",
                schema: "club",
                table: "EventRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationJwt",
                schema: "club",
                table: "EventRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScanCount",
                schema: "club",
                table: "EventRegistrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TicketId",
                schema: "club",
                table: "EventRegistrations",
                type: "text",
                nullable: true);

            // Backfill existing rows BEFORE adding unique index/NOT NULL constraints.
            // We avoid extensions (e.g. pgcrypto) by using md5 + event/registration ids for uniqueness.
            migrationBuilder.Sql(@"
UPDATE club.""EventRegistrations""
SET ""TicketId"" = 'TCK-' || upper(substr(md5(""Id""::text || '-' || ""EventId""::text || '-' || ""UserId""::text), 1, 16))
WHERE ""TicketId"" IS NULL OR length(trim(""TicketId"")) = 0;
");

            migrationBuilder.Sql(@"
UPDATE club.""EventRegistrations""
SET ""RegistrationJwt"" = 'migrated'
WHERE ""RegistrationJwt"" IS NULL OR length(trim(""RegistrationJwt"")) = 0;
");

            migrationBuilder.AlterColumn<string>(
                name: "TicketId",
                schema: "club",
                table: "EventRegistrations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationJwt",
                schema: "club",
                table: "EventRegistrations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_TicketId",
                schema: "club",
                table: "EventRegistrations",
                column: "TicketId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventRegistrations_TicketId",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "CheckedInBy",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "EventEndTimeSnapshot",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "EventImageUrlSnapshot",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "EventLocationSnapshot",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "EventNameSnapshot",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "EventSeatLimitSnapshot",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "EventStartTimeSnapshot",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "IsCheckedIn",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "LastScannedAt",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "RegistrationJwt",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "ScanCount",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "TicketId",
                schema: "club",
                table: "EventRegistrations");
        }
    }
}

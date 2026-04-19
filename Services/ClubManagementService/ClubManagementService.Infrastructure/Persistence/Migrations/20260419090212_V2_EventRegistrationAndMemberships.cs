using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V2_EventRegistrationAndMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // uuid EventId cannot be cast to int — clear rows and replace column.
            migrationBuilder.Sql(@"TRUNCATE TABLE club.""EventRegistrations"" RESTART IDENTITY;");
            migrationBuilder.Sql(@"ALTER TABLE club.""EventRegistrations"" DROP COLUMN ""EventId"";");
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                schema: "club",
                table: "EventRegistrations",
                type: "integer",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "ClubMemberships",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubMemberships_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalSchema: "club",
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobVacancies_ClubId",
                schema: "club",
                table: "JobVacancies",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobVacancyId",
                schema: "club",
                table: "JobApplications",
                column: "JobVacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_EventId_UserId",
                schema: "club",
                table: "EventRegistrations",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_ClubId_UserId",
                schema: "club",
                table: "ClubMemberships",
                columns: new[] { "ClubId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EventRegistrations_Events_EventId",
                schema: "club",
                table: "EventRegistrations",
                column: "EventId",
                principalSchema: "club",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobApplications_JobVacancies_JobVacancyId",
                schema: "club",
                table: "JobApplications",
                column: "JobVacancyId",
                principalSchema: "club",
                principalTable: "JobVacancies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobVacancies_Clubs_ClubId",
                schema: "club",
                table: "JobVacancies",
                column: "ClubId",
                principalSchema: "club",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventRegistrations_Events_EventId",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_JobApplications_JobVacancies_JobVacancyId",
                schema: "club",
                table: "JobApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_JobVacancies_Clubs_ClubId",
                schema: "club",
                table: "JobVacancies");

            migrationBuilder.DropTable(
                name: "ClubMemberships",
                schema: "club");

            migrationBuilder.DropIndex(
                name: "IX_JobVacancies_ClubId",
                schema: "club",
                table: "JobVacancies");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_JobVacancyId",
                schema: "club",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_EventRegistrations_EventId_UserId",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "EventId",
                schema: "club",
                table: "EventRegistrations");

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                schema: "club",
                table: "EventRegistrations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}

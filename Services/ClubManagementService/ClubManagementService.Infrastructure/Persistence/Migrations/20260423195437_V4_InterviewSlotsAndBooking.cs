using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V4_InterviewSlotsAndBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterviewSlotId",
                schema: "club",
                table: "JobApplications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InterviewSlots",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SlotId = table.Column<string>(type: "text", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false),
                    JobVacancyId = table.Column<int>(type: "integer", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_InterviewSlots", x => x.Id);
                    table.UniqueConstraint("AK_InterviewSlots_SlotId", x => x.SlotId);
                    table.ForeignKey(
                        name: "FK_InterviewSlots_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalSchema: "club",
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewSlots_JobVacancies_JobVacancyId",
                        column: x => x.JobVacancyId,
                        principalSchema: "club",
                        principalTable: "JobVacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_InterviewSlotId",
                schema: "club",
                table: "JobApplications",
                column: "InterviewSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSlots_ClubId_StartsAt",
                schema: "club",
                table: "InterviewSlots",
                columns: new[] { "ClubId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSlots_JobVacancyId",
                schema: "club",
                table: "InterviewSlots",
                column: "JobVacancyId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobApplications_InterviewSlots_InterviewSlotId",
                schema: "club",
                table: "JobApplications",
                column: "InterviewSlotId",
                principalSchema: "club",
                principalTable: "InterviewSlots",
                principalColumn: "SlotId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobApplications_InterviewSlots_InterviewSlotId",
                schema: "club",
                table: "JobApplications");

            migrationBuilder.DropTable(
                name: "InterviewSlots",
                schema: "club");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_InterviewSlotId",
                schema: "club",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewSlotId",
                schema: "club",
                table: "JobApplications");
        }
    }
}

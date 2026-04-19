using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClubPositionsAndVacancyPositionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationDeadline",
                schema: "club",
                table: "JobVacancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClubPositionId",
                schema: "club",
                table: "JobVacancies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                schema: "club",
                table: "JobVacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ClubPositions",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ClubPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubPositions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "club",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubPositions_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalSchema: "club",
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubPositionRequirements",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubPositionId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubPositionRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubPositionRequirements_ClubPositions_ClubPositionId",
                        column: x => x.ClubPositionId,
                        principalSchema: "club",
                        principalTable: "ClubPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobVacancies_ClubPositionId",
                schema: "club",
                table: "JobVacancies",
                column: "ClubPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubPositionRequirements_ClubPositionId",
                schema: "club",
                table: "ClubPositionRequirements",
                column: "ClubPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubPositions_CategoryId",
                schema: "club",
                table: "ClubPositions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubPositions_ClubId",
                schema: "club",
                table: "ClubPositions",
                column: "ClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobVacancies_ClubPositions_ClubPositionId",
                schema: "club",
                table: "JobVacancies",
                column: "ClubPositionId",
                principalSchema: "club",
                principalTable: "ClubPositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobVacancies_ClubPositions_ClubPositionId",
                schema: "club",
                table: "JobVacancies");

            migrationBuilder.DropTable(
                name: "ClubPositionRequirements",
                schema: "club");

            migrationBuilder.DropTable(
                name: "ClubPositions",
                schema: "club");

            migrationBuilder.DropIndex(
                name: "IX_JobVacancies_ClubPositionId",
                schema: "club",
                table: "JobVacancies");

            migrationBuilder.DropColumn(
                name: "ApplicationDeadline",
                schema: "club",
                table: "JobVacancies");

            migrationBuilder.DropColumn(
                name: "ClubPositionId",
                schema: "club",
                table: "JobVacancies");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                schema: "club",
                table: "JobVacancies");
        }
    }
}

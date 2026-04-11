using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LostFoundService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnifiedLostFoundApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LostFoundItems",
                schema: "lostfound",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Location = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AdminStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrimaryImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PostedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    BuildingCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsRoom = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RoomOrArea = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CampusLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Building = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Floor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RoomArea = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastKnownLocation = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    DateLost = table.Column<DateOnly>(type: "date", nullable: true),
                    TimeLost = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CollectionPlace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StorageBinId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Condition = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AdminNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReportedColor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IntakePhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConfirmAccuracy = table.Column<bool>(type: "boolean", nullable: true),
                    ClaimantName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ClaimantStudentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StudentIdVerified = table.Column<bool>(type: "boolean", nullable: true),
                    HandoverProofUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveryCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostFoundItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LostFoundItemClaims",
                schema: "lostfound",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LostFoundItemId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClaimType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostFoundItemClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LostFoundItemClaims_LostFoundItems_LostFoundItemId",
                        column: x => x.LostFoundItemId,
                        principalSchema: "lostfound",
                        principalTable: "LostFoundItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LostFoundItemImages",
                schema: "lostfound",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LostFoundItemId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostFoundItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LostFoundItemImages_LostFoundItems_LostFoundItemId",
                        column: x => x.LostFoundItemId,
                        principalSchema: "lostfound",
                        principalTable: "LostFoundItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LostFoundItemClaims_LostFoundItemId_UserId_ClaimType",
                schema: "lostfound",
                table: "LostFoundItemClaims",
                columns: new[] { "LostFoundItemId", "UserId", "ClaimType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LostFoundItemImages_LostFoundItemId",
                schema: "lostfound",
                table: "LostFoundItemImages",
                column: "LostFoundItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LostFoundItems_ReferenceNumber",
                schema: "lostfound",
                table: "LostFoundItems",
                column: "ReferenceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LostFoundItemClaims",
                schema: "lostfound");

            migrationBuilder.DropTable(
                name: "LostFoundItemImages",
                schema: "lostfound");

            migrationBuilder.DropTable(
                name: "LostFoundItems",
                schema: "lostfound");
        }
    }
}

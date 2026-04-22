using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations;

public partial class AddEventProposals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventProposals",
            schema: "club",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ClubNumericId = table.Column<int>(type: "integer", nullable: true),
                ClubRouteKey = table.Column<string>(type: "text", nullable: true),
                ClubName = table.Column<string>(type: "text", nullable: false),
                BodyJson = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedEventId = table.Column<int>(type: "integer", nullable: true),
                LastApproveAssignmentsJson = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventProposals", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EventProposals_Status",
            schema: "club",
            table: "EventProposals",
            column: "Status");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ArchivedAt",
            schema: "club",
            table: "EventProposals",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ArchivedAt",
            schema: "club",
            table: "EventProposals");

        migrationBuilder.DropTable(
            name: "EventProposals",
            schema: "club");
    }
}


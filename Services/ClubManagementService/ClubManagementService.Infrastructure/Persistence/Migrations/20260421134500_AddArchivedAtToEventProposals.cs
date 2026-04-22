using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations;

public partial class AddArchivedAtToEventProposals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE club."EventProposals"
            ADD COLUMN IF NOT EXISTS "ArchivedAt" timestamp with time zone;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE club."EventProposals"
            DROP COLUMN IF EXISTS "ArchivedAt";
            """);
    }
}


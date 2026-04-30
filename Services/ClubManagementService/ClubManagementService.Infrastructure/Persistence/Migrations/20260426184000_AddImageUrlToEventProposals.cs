using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ClubManagementDbContext))]
[Migration("20260426184000_AddImageUrlToEventProposals")]
public partial class AddImageUrlToEventProposals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE club."EventProposals"
            ADD COLUMN IF NOT EXISTS "ImageUrl" text;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE club."EventProposals"
            DROP COLUMN IF EXISTS "ImageUrl";
            """);
    }
}

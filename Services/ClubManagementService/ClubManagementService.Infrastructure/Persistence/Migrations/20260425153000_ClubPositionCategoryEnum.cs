using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ClubManagementDbContext))]
    [Migration("20260425153000_ClubPositionCategoryEnum")]
    public partial class ClubPositionCategoryEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "club",
                table: "ClubPositions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.Sql(
                """
                UPDATE club."ClubPositions"
                SET "Category" = CASE
                    WHEN "Title" ILIKE ANY (ARRAY['%finance%', '%treasurer%', '%budget%']) THEN 'Finance'
                    WHEN "Title" ILIKE ANY (ARRAY['%marketing%', '%brand%', '%promotion%', '%social media%', '%sponsor%', '%partnership%']) THEN 'Marketing'
                    WHEN "Title" ILIKE ANY (ARRAY['%tech%', '%technical%', '%engineering%', '%developer%', '%software%', '%data%', '%workshop%']) THEN 'Technology'
                    WHEN "Title" ILIKE ANY (ARRAY['%outreach%', '%volunteer%', '%liaison%', '%mentor%', '%mentorship%', '%community%', '%ambassador%', '%delegate%']) THEN 'Outreach'
                    WHEN "Title" ILIKE ANY (ARRAY['%content%', '%editor%', '%media%', '%photograph%', '%photo%', '%design%', '%video%', '%subtitle%', '%sound%', '%creative%', '%sketch%', '%choreograph%', '%writer%', '%communications%']) THEN 'Content'
                    WHEN "Title" ILIKE ANY (ARRAY['%event%', '%logistics%', '%coordinator%', '%director%', '%curator%', '%referee%', '%host%', '%organizer%']) THEN 'Events'
                    WHEN "CategoryId" = 1 THEN 'Technology'
                    WHEN "CategoryId" = 2 THEN 'Content'
                    WHEN "CategoryId" = 3 THEN 'Finance'
                    WHEN "CategoryId" = 4 THEN 'Events'
                    WHEN "CategoryId" = 5 THEN 'Outreach'
                    WHEN "CategoryId" = 6 THEN 'Content'
                    WHEN "CategoryId" = 7 THEN 'Content'
                    WHEN "CategoryId" = 8 THEN 'Outreach'
                    WHEN "CategoryId" = 9 THEN 'Outreach'
                    WHEN "CategoryId" = 10 THEN 'Marketing'
                    ELSE 'Other'
                END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ClubPositions_Categories_CategoryId",
                schema: "club",
                table: "ClubPositions");

            migrationBuilder.DropIndex(
                name: "IX_ClubPositions_CategoryId",
                schema: "club",
                table: "ClubPositions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "club",
                table: "ClubPositions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                schema: "club",
                table: "ClubPositions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE club."ClubPositions"
                SET "CategoryId" = CASE "Category"
                    WHEN 'Technology' THEN 1
                    WHEN 'Events' THEN 4
                    WHEN 'Finance' THEN 3
                    WHEN 'Outreach' THEN 5
                    WHEN 'Content' THEN 6
                    WHEN 'Marketing' THEN 10
                    ELSE 10
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ClubPositions_CategoryId",
                schema: "club",
                table: "ClubPositions",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubPositions_Categories_CategoryId",
                schema: "club",
                table: "ClubPositions",
                column: "CategoryId",
                principalSchema: "club",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "club",
                table: "ClubPositions");
        }
    }
}

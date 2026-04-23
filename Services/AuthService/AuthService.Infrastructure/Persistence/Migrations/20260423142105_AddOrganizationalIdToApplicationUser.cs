using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationalIdToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationalId",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE auth."AspNetUsers" AS u
                SET "OrganizationalId" = generated."OrganizationalId"
                FROM (
                    SELECT "Id",
                           LPAD((100000000 + ROW_NUMBER() OVER (ORDER BY "Id"))::text, 9, '0') AS "OrganizationalId"
                    FROM auth."AspNetUsers"
                ) AS generated
                WHERE u."Id" = generated."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationalId",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationalId",
                schema: "auth",
                table: "AspNetUsers",
                column: "OrganizationalId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AspNetUsers_OrganizationalId_9Digits",
                schema: "auth",
                table: "AspNetUsers",
                sql: "\"OrganizationalId\" ~ '^[0-9]{9}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizationalId",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AspNetUsers_OrganizationalId_9Digits",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationalId",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}

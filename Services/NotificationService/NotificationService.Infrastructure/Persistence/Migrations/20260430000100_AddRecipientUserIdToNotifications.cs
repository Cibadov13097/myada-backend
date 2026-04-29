using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NotificationService.Infrastructure.Persistence;

#nullable disable

namespace NotificationService.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(NotificationDbContext))]
    [Migration("20260430000100_AddRecipientUserIdToNotifications")]
    public partial class AddRecipientUserIdToNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecipientUserId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientUserId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RecipientUserId",
                table: "Notifications");
        }
    }
}

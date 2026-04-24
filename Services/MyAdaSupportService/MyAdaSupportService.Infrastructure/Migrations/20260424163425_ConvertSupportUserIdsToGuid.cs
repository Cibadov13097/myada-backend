using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAdaSupportService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertSupportUserIdsToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "SupportStaffStatuses");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "SupportStaffStatuses");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedUserId",
                table: "SupportStaffStatuses",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedUserId",
                table: "SupportStaffStatuses",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AssignedBy",
                table: "Requests");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedBy",
                table: "Requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedUserId",
                table: "Requests",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedUserId",
                table: "Requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StaffId",
                table: "Requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Categories");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedUserId",
                table: "Categories",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedUserId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedUserId",
                table: "Attachments",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedUserId",
                table: "Attachments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "SupportStaffStatuses");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "SupportStaffStatuses");

            migrationBuilder.AddColumn<int>(
                name: "CreatedUserId",
                table: "SupportStaffStatuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedUserId",
                table: "SupportStaffStatuses",
                type: "integer",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AssignedBy",
                table: "Requests");

            migrationBuilder.AddColumn<int>(
                name: "AssignedBy",
                table: "Requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedUserId",
                table: "Requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedUserId",
                table: "Requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StaffId",
                table: "Requests",
                type: "integer",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Categories");

            migrationBuilder.AddColumn<int>(
                name: "CreatedUserId",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedUserId",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Attachments");

            migrationBuilder.AddColumn<int>(
                name: "CreatedUserId",
                table: "Attachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedUserId",
                table: "Attachments",
                type: "integer",
                nullable: true);
        }
    }
}

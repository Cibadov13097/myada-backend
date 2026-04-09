using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClubManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class clubProposalsCreatedEdited : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_Categories_CategoryId",
                schema: "club",
                table: "Clubs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subevents",
                schema: "club",
                table: "Subevents");

            migrationBuilder.DropColumn(
                name: "EndTime",
                schema: "club",
                table: "Subevents");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                schema: "club",
                table: "Clubs");

            migrationBuilder.RenameTable(
                name: "Subevents",
                schema: "club",
                newName: "SubEvents",
                newSchema: "club");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                schema: "club",
                table: "SubEvents",
                newName: "StartDateTime");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "club",
                table: "SubEvents",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "MainEventId",
                schema: "club",
                table: "SubEvents",
                newName: "EventId1");

            migrationBuilder.RenameColumn(
                name: "ApprovedBy",
                schema: "club",
                table: "Events",
                newName: "RoomId");

            migrationBuilder.RenameColumn(
                name: "Description",
                schema: "club",
                table: "Clubs",
                newName: "Email");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                schema: "club",
                table: "SubEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                schema: "club",
                table: "SubEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                schema: "club",
                table: "SubEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                schema: "club",
                table: "SubEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                schema: "club",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "club",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                schema: "club",
                table: "Events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ClubId1",
                schema: "club",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                schema: "club",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                schema: "club",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateTime",
                schema: "club",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "club",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                schema: "club",
                table: "Clubs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "BackgroundImageUrl",
                schema: "club",
                table: "Clubs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "club",
                table: "Clubs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                schema: "club",
                table: "Clubs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                schema: "club",
                table: "Clubs",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubEvents",
                schema: "club",
                table: "SubEvents",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ClubMembers",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    DateJoined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClubId1 = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ClubMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubMembers_Clubs_ClubId1",
                        column: x => x.ClubId1,
                        principalSchema: "club",
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubProposals",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProposedClubName = table.Column<string>(type: "text", nullable: false),
                    ShortDescription = table.Column<string>(type: "text", nullable: false),
                    UniqueValue = table.Column<string>(type: "text", nullable: false),
                    MainGoalsAndObjectives = table.Column<string>(type: "text", nullable: false),
                    ProposedActivitiesAndEvents = table.Column<string>(type: "text", nullable: false),
                    AlignmentWithUniversityMission = table.Column<string>(type: "text", nullable: false),
                    LongTermVision = table.Column<string>(type: "text", nullable: false),
                    CommitmentAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    PresidentStudentId = table.Column<string>(type: "text", nullable: false),
                    VicePresidentStudentId = table.Column<string>(type: "text", nullable: false),
                    ClubLogoUrl = table.Column<string>(type: "text", nullable: true),
                    ClubConstitutionUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ClubProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventParticipants",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<string>(type: "text", nullable: false),
                    QRInfo = table.Column<string>(type: "text", nullable: false),
                    EventId1 = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_EventParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventParticipants_Events_EventId1",
                        column: x => x.EventId1,
                        principalSchema: "club",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventProposals",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    Attendance = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    PosterUrl = table.Column<string>(type: "text", nullable: true),
                    IsAudioVisualNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    IsSecurityNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    IsCateringNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    IsCleaningNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    OtherRequirement = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_EventProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    ClubId1 = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Clubs_ClubId1",
                        column: x => x.ClubId1,
                        principalSchema: "club",
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subevents",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MainEventId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_Subevents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClubProposalExecutiveMembers",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: false),
                    ClubProposalId1 = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ClubProposalExecutiveMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubProposalExecutiveMembers_ClubProposals_ClubProposalId1",
                        column: x => x.ClubProposalId1,
                        principalSchema: "club",
                        principalTable: "ClubProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposedSubEvents",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    ProposedRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    EventProposalId1 = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ProposedSubEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposedSubEvents_EventProposals_EventProposalId1",
                        column: x => x.EventProposalId1,
                        principalSchema: "club",
                        principalTable: "EventProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubStaff",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateJoined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClubId1 = table.Column<int>(type: "integer", nullable: false),
                    PositionId1 = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ClubStaff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubStaff_Clubs_ClubId1",
                        column: x => x.ClubId1,
                        principalSchema: "club",
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClubStaff_Positions_PositionId1",
                        column: x => x.PositionId1,
                        principalSchema: "club",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionRequirements",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Requirement = table.Column<string>(type: "text", nullable: false),
                    PositionId1 = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PositionRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionRequirements_Positions_PositionId1",
                        column: x => x.PositionId1,
                        principalSchema: "club",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vacancies",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeadlineDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PositionId1 = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Vacancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vacancies_Positions_PositionId1",
                        column: x => x.PositionId1,
                        principalSchema: "club",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyApplications",
                schema: "club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VacancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<string>(type: "text", nullable: false),
                    ApplicationPurpose = table.Column<string>(type: "text", nullable: false),
                    CVUrl = table.Column<string>(type: "text", nullable: false),
                    VacancyId1 = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_VacancyApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyApplications_Vacancies_VacancyId1",
                        column: x => x.VacancyId1,
                        principalSchema: "club",
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubEvents_EventId1",
                schema: "club",
                table: "SubEvents",
                column: "EventId1");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ClubId1",
                schema: "club",
                table: "Events",
                column: "ClubId1");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembers_ClubId1",
                schema: "club",
                table: "ClubMembers",
                column: "ClubId1");

            migrationBuilder.CreateIndex(
                name: "IX_ClubProposalExecutiveMembers_ClubProposalId1",
                schema: "club",
                table: "ClubProposalExecutiveMembers",
                column: "ClubProposalId1");

            migrationBuilder.CreateIndex(
                name: "IX_ClubStaff_ClubId1",
                schema: "club",
                table: "ClubStaff",
                column: "ClubId1");

            migrationBuilder.CreateIndex(
                name: "IX_ClubStaff_PositionId1",
                schema: "club",
                table: "ClubStaff",
                column: "PositionId1");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_EventId1",
                schema: "club",
                table: "EventParticipants",
                column: "EventId1");

            migrationBuilder.CreateIndex(
                name: "IX_PositionRequirements_PositionId1",
                schema: "club",
                table: "PositionRequirements",
                column: "PositionId1");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_ClubId1",
                schema: "club",
                table: "Positions",
                column: "ClubId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedSubEvents_EventProposalId1",
                schema: "club",
                table: "ProposedSubEvents",
                column: "EventProposalId1");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_PositionId1",
                schema: "club",
                table: "Vacancies",
                column: "PositionId1");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyApplications_VacancyId1",
                schema: "club",
                table: "VacancyApplications",
                column: "VacancyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_Categories_CategoryId",
                schema: "club",
                table: "Clubs",
                column: "CategoryId",
                principalSchema: "club",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Clubs_ClubId1",
                schema: "club",
                table: "Events",
                column: "ClubId1",
                principalSchema: "club",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubEvents_Events_EventId1",
                schema: "club",
                table: "SubEvents",
                column: "EventId1",
                principalSchema: "club",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_Categories_CategoryId",
                schema: "club",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Clubs_ClubId1",
                schema: "club",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_SubEvents_Events_EventId1",
                schema: "club",
                table: "SubEvents");

            migrationBuilder.DropTable(
                name: "ClubMembers",
                schema: "club");

            migrationBuilder.DropTable(
                name: "ClubProposalExecutiveMembers",
                schema: "club");

            migrationBuilder.DropTable(
                name: "ClubStaff",
                schema: "club");

            migrationBuilder.DropTable(
                name: "EventParticipants",
                schema: "club");

            migrationBuilder.DropTable(
                name: "PositionRequirements",
                schema: "club");

            migrationBuilder.DropTable(
                name: "ProposedSubEvents",
                schema: "club");

            migrationBuilder.DropTable(
                name: "Subevents",
                schema: "club");

            migrationBuilder.DropTable(
                name: "VacancyApplications",
                schema: "club");

            migrationBuilder.DropTable(
                name: "ClubProposals",
                schema: "club");

            migrationBuilder.DropTable(
                name: "EventProposals",
                schema: "club");

            migrationBuilder.DropTable(
                name: "Vacancies",
                schema: "club");

            migrationBuilder.DropTable(
                name: "Positions",
                schema: "club");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubEvents",
                schema: "club",
                table: "SubEvents");

            migrationBuilder.DropIndex(
                name: "IX_SubEvents_EventId1",
                schema: "club",
                table: "SubEvents");

            migrationBuilder.DropIndex(
                name: "IX_Events_ClubId1",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Capacity",
                schema: "club",
                table: "SubEvents");

            migrationBuilder.DropColumn(
                name: "Duration",
                schema: "club",
                table: "SubEvents");

            migrationBuilder.DropColumn(
                name: "EventId",
                schema: "club",
                table: "SubEvents");

            migrationBuilder.DropColumn(
                name: "RoomId",
                schema: "club",
                table: "SubEvents");

            migrationBuilder.DropColumn(
                name: "Capacity",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ClubId",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ClubId1",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Duration",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "StartDateTime",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "club",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BackgroundImageUrl",
                schema: "club",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "club",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                schema: "club",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                schema: "club",
                table: "Clubs");

            migrationBuilder.RenameTable(
                name: "SubEvents",
                schema: "club",
                newName: "Subevents",
                newSchema: "club");

            migrationBuilder.RenameColumn(
                name: "Title",
                schema: "club",
                table: "Subevents",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "StartDateTime",
                schema: "club",
                table: "Subevents",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "EventId1",
                schema: "club",
                table: "Subevents",
                newName: "MainEventId");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                schema: "club",
                table: "Events",
                newName: "ApprovedBy");

            migrationBuilder.RenameColumn(
                name: "Email",
                schema: "club",
                table: "Clubs",
                newName: "Description");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                schema: "club",
                table: "Subevents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                schema: "club",
                table: "Clubs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                schema: "club",
                table: "Clubs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subevents",
                schema: "club",
                table: "Subevents",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_Categories_CategoryId",
                schema: "club",
                table: "Clubs",
                column: "CategoryId",
                principalSchema: "club",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

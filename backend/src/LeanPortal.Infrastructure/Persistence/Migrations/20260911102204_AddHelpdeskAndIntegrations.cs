using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeanPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpdeskAndIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HelpdeskAttempts",
                table: "ContactMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HelpdeskLastError",
                table: "ContactMessages",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HelpdeskNextAttemptAt",
                table: "ContactMessages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HelpdeskSentAt",
                table: "ContactMessages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HelpdeskStatus",
                table: "ContactMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HelpdeskTicketId",
                table: "ContactMessages",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelpdeskTicketNumber",
                table: "ContactMessages",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueCategory",
                table: "ContactMessages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueSubCategory",
                table: "ContactMessages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueType",
                table: "ContactMessages",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserType",
                table: "ContactMessages",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentTexts",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExtractedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTexts", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_DocumentTexts_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIntegrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Intro = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EmbedCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FrameHeight = table.Column<int>(type: "int", nullable: false),
                    ApiUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApiMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ApiHeaderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApiHeaderValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApiBodyTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiResultPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApiFieldMap = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiTotalPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastCheckSucceeded = table.Column<bool>(type: "bit", nullable: true),
                    LastCheckResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIntegrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HelpdeskConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    AccountsUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrganisationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DepartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientSecret = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ContactOwnerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TicketTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrievanceMatrix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SendAttachments = table.Column<bool>(type: "bit", nullable: false),
                    AlsoSendEmail = table.Column<bool>(type: "bit", nullable: false),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastCheckSucceeded = table.Column<bool>(type: "bit", nullable: true),
                    LastCheckResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpdeskConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpdeskConnections_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_HelpdeskStatus_HelpdeskNextAttemptAt",
                table: "ContactMessages",
                columns: new[] { "HelpdeskStatus", "HelpdeskNextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntegrations_Key",
                table: "ExternalIntegrations",
                column: "Key",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HelpdeskConnections_PartnerId",
                table: "HelpdeskConnections",
                column: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentTexts");

            migrationBuilder.DropTable(
                name: "ExternalIntegrations");

            migrationBuilder.DropTable(
                name: "HelpdeskConnections");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_HelpdeskStatus_HelpdeskNextAttemptAt",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "HelpdeskAttempts",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "HelpdeskLastError",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "HelpdeskNextAttemptAt",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "HelpdeskSentAt",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "HelpdeskStatus",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "HelpdeskTicketId",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "HelpdeskTicketNumber",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "IssueCategory",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "IssueSubCategory",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "IssueType",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "ContactMessages");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeanPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncentives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incentives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    State = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuerName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IssuerLogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Document1Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Document1Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Document2Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Document2Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AvailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AvailLabel = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incentives", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incentives_Category_IsActive_SortOrder",
                table: "Incentives",
                columns: new[] { "Category", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Incentives_State",
                table: "Incentives",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incentives");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeanPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncentiveVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Incentives",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Incentives");
        }
    }
}

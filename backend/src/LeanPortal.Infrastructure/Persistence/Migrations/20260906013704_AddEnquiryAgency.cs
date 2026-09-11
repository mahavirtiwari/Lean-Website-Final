using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeanPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnquiryAgency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Agency",
                table: "ContactMessages",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Agency",
                table: "ContactMessages");
        }
    }
}

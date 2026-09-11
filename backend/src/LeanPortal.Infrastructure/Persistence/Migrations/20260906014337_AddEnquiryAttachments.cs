using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeanPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnquiryAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentsJson",
                table: "ContactMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentsJson",
                table: "ContactMessages");
        }
    }
}

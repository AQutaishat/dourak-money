using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dourak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCircleMemberInviteToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                table: "CircleMembers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "CircleMembers");
        }
    }
}

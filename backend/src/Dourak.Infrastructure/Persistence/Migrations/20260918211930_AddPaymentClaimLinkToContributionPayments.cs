using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dourak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentClaimLinkToContributionPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentClaimId",
                table: "ContributionPayments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentClaimId",
                table: "ContributionPayments");
        }
    }
}

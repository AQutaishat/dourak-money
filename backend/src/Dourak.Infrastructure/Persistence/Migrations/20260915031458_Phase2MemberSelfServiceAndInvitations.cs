using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dourak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2MemberSelfServiceAndInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CircleMembers_CircleId",
                table: "CircleMembers");

            migrationBuilder.AddColumn<int>(
                name: "InvitationStatus",
                table: "CircleMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvitedAt",
                table: "CircleMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RespondedAt",
                table: "CircleMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CircleMembers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedDisplayName",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPhoneNumber",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContributionId = table.Column<int>(type: "integer", nullable: false),
                    CircleId = table.Column<int>(type: "integer", nullable: false),
                    MemberId = table.Column<int>(type: "integer", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ClaimedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EvidenceStoredFileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EvidenceOriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    EvidenceContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EvidenceSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentClaims_CircleMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "CircleMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentClaims_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CircleMembers_CircleId_UserId",
                table: "CircleMembers",
                columns: new[] { "CircleId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CircleMembers_UserId",
                table: "CircleMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NormalizedDisplayName",
                table: "AspNetUsers",
                column: "NormalizedDisplayName",
                unique: true,
                filter: "\"NormalizedDisplayName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NormalizedPhoneNumber",
                table: "AspNetUsers",
                column: "NormalizedPhoneNumber",
                unique: true,
                filter: "\"NormalizedPhoneNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentClaims_CircleId_Status",
                table: "PaymentClaims",
                columns: new[] { "CircleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentClaims_ContributionId",
                table: "PaymentClaims",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentClaims_MemberId",
                table: "PaymentClaims",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentClaims_SubmittedByUserId",
                table: "PaymentClaims",
                column: "SubmittedByUserId");

            // Backfill the normalized columns for accounts that existed before Phase 2, so the
            // new uniqueness indexes (prompt02 §8) also cover them instead of only new/edited
            // profiles. Mirrors UserValueNormalizer: upper-cased trimmed name, digits-only phone
            // keeping a leading "+".
            // Pre-Phase-2 data may already contain two accounts sharing a name/phone; those rows
            // are left NULL (the filtered index ignores NULLs) rather than failing the migration.
            // The owners simply have to pick a unique value the next time they save their profile.
            migrationBuilder.Sql("""
                WITH normalized AS (
                    SELECT "Id", UPPER(TRIM("DisplayName")) AS n
                    FROM "AspNetUsers"
                    WHERE "DisplayName" IS NOT NULL AND TRIM("DisplayName") <> ''
                ), unique_only AS (
                    SELECT n FROM normalized GROUP BY n HAVING COUNT(*) = 1
                )
                UPDATE "AspNetUsers" u
                SET "NormalizedDisplayName" = nz.n
                FROM normalized nz
                WHERE u."Id" = nz."Id" AND nz.n IN (SELECT n FROM unique_only);

                WITH normalized AS (
                    SELECT "Id",
                           CASE WHEN TRIM("PhoneNumber") LIKE '+%' THEN '+' ELSE '' END
                           || REGEXP_REPLACE("PhoneNumber", '[^0-9]', '', 'g') AS n
                    FROM "AspNetUsers"
                    WHERE "PhoneNumber" IS NOT NULL AND REGEXP_REPLACE("PhoneNumber", '[^0-9]', '', 'g') <> ''
                ), unique_only AS (
                    SELECT n FROM normalized GROUP BY n HAVING COUNT(*) = 1
                )
                UPDATE "AspNetUsers" u
                SET "NormalizedPhoneNumber" = nz.n
                FROM normalized nz
                WHERE u."Id" = nz."Id" AND nz.n IN (SELECT n FROM unique_only);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentClaims");

            migrationBuilder.DropIndex(
                name: "IX_CircleMembers_CircleId_UserId",
                table: "CircleMembers");

            migrationBuilder.DropIndex(
                name: "IX_CircleMembers_UserId",
                table: "CircleMembers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NormalizedDisplayName",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NormalizedPhoneNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvitationStatus",
                table: "CircleMembers");

            migrationBuilder.DropColumn(
                name: "InvitedAt",
                table: "CircleMembers");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "CircleMembers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CircleMembers");

            migrationBuilder.DropColumn(
                name: "NormalizedDisplayName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedPhoneNumber",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CircleMembers_CircleId",
                table: "CircleMembers",
                column: "CircleId");
        }
    }
}

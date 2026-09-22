using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dourak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AttachmentStoredFileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AttachmentOriginalFileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_CreatedAt",
                table: "SupportRequests",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportRequests");
        }
    }
}

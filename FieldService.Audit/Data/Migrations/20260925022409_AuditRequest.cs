using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiledService.Audit.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JwtId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IpAddressHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserAgentHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Resource = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Successful = table.Column<bool>(type: "boolean", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRequest", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_SessionActivity_Timestamp",
                table: "AuditRequest",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditRequest");
        }
    }
}

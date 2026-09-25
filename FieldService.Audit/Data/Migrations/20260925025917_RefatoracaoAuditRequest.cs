using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiledService.Audit.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefatoracaoAuditRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "AuditRequest",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "AuditRequest",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AuditRequest");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AuditRequest");
        }
    }
}

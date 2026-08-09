using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiledService.Audit.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Resource = table.Column<string>(type: "text", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Parameters = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditAccesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResourceId = table.Column<string>(type: "text", nullable: false),
                    Resource = table.Column<string>(type: "text", nullable: false),
                    ChangedProperties = table.Column<JsonDocument>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_AuditAccesses_OccurredAt",
                table: "AuditAccesses",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "idx_AuditAccesses_RequestId",
                table: "AuditAccesses",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "idx_AuditAccesses_UserId_TenantId",
                table: "AuditAccesses",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "idx_AuditChanges_OccurredAt",
                table: "AuditChanges",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "idx_AuditChanges_RequestId",
                table: "AuditChanges",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "idx_AuditChanges_UserId_TenantId",
                table: "AuditChanges",
                columns: new[] { "UserId", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditAccesses");

            migrationBuilder.DropTable(
                name: "AuditChanges");
        }
    }
}

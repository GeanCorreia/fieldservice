using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiledService.Audit.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditDtoSchemaRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Resource",
                table: "AuditAccesses",
                newName: "ResourceName");

            migrationBuilder.AddColumn<string>(
                name: "SchemaVersion",
                table: "AuditAccesses",
                type: "text",
                nullable: false,
                defaultValue: "1.0.0");

            migrationBuilder.CreateTable(
                name: "AuditDtoSchemas",
                columns: table => new
                {
                    ResourceName = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Properties = table.Column<JsonDocument>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditDtoSchemas", x => new { x.ResourceName, x.Version });
                });

            migrationBuilder.CreateIndex(
                name: "idx_AuditAccesses_ResourceName_SchemaVersion",
                table: "AuditAccesses",
                columns: new[] { "ResourceName", "SchemaVersion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditDtoSchemas");

            migrationBuilder.DropIndex(
                name: "idx_AuditAccesses_ResourceName_SchemaVersion",
                table: "AuditAccesses");

            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                table: "AuditAccesses");

            migrationBuilder.RenameColumn(
                name: "ResourceName",
                table: "AuditAccesses",
                newName: "Resource");
        }
    }
}

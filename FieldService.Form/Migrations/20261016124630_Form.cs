using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Form.Migrations
{
    /// <inheritdoc />
    public partial class Form : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "form");

            migrationBuilder.CreateTable(
                name: "Form",
                schema: "form",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Form", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormTenant",
                schema: "form",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionStringId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeveloperMcpEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FormExecutionEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormEvent",
                schema: "form",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaJson = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormEvent_Form_FormId",
                        column: x => x.FormId,
                        principalSchema: "form",
                        principalTable: "Form",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Form_TenantId",
                schema: "form",
                table: "Form",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Form_TenantId_Id",
                schema: "form",
                table: "Form",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_FormEvent_FormId",
                schema: "form",
                table: "FormEvent",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormEvent_FormId_OccurredAt",
                schema: "form",
                table: "FormEvent",
                columns: new[] { "FormId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormTenant_ConnectionStringId",
                schema: "form",
                table: "FormTenant",
                column: "ConnectionStringId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTenant_TenantId",
                schema: "form",
                table: "FormTenant",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormEvent",
                schema: "form");

            migrationBuilder.DropTable(
                name: "FormTenant",
                schema: "form");

            migrationBuilder.DropTable(
                name: "Form",
                schema: "form");
        }
    }
}

using System;
using System.Text.Json;
using FieldService.Form.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Form.Data.Migrations;

[DbContext(typeof(FormDataDbContext))]
[Migration("20261008000000_InitialForm")]
public partial class InitialForm : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "form");

        migrationBuilder.CreateTable(
            name: "FormSubmission",
            schema: "form",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FormId = table.Column<Guid>(type: "uuid", nullable: false),
                DataJson = table.Column<JsonElement>(type: "jsonb", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeleteByUserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FormSubmission", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FormSubmission_FormId",
            schema: "form",
            table: "FormSubmission",
            column: "FormId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "FormSubmission",
            schema: "form");
    }
}



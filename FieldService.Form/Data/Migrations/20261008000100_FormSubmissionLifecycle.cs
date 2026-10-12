using System;
using FieldService.Form.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Form.Data.Migrations;

[DbContext(typeof(FormDataDbContext))]
[Migration("20261008000100_FormSubmissionLifecycle")]
public partial class FormSubmissionLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            schema: "form",
            table: "FormSubmission",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.AddColumn<bool>(
            name: "IsDelete",
            schema: "form",
            table: "FormSubmission",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(@"
UPDATE form.""FormSubmission""
SET ""IsDelete"" = TRUE,
    ""LastModifiedAt"" = COALESCE(""DeletedAt"", ""LastModifiedAt"")
WHERE ""DeletedAt"" IS NOT NULL;");

        migrationBuilder.DropColumn(
            name: "DeletedAt",
            schema: "form",
            table: "FormSubmission");

        migrationBuilder.CreateIndex(
            name: "IX_FormSubmission_FormId_IsDelete_LastModifiedAt",
            schema: "form",
            table: "FormSubmission",
            columns: new[] { "FormId", "IsDelete", "LastModifiedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FormSubmission_FormId_IsDelete_LastModifiedAt",
            schema: "form",
            table: "FormSubmission");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeletedAt",
            schema: "form",
            table: "FormSubmission",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql(@"
UPDATE form.""FormSubmission""
SET ""DeletedAt"" = ""LastModifiedAt""
WHERE ""IsDelete"" = TRUE;");

        migrationBuilder.DropColumn(
            name: "IsDelete",
            schema: "form",
            table: "FormSubmission");

        migrationBuilder.DropColumn(
            name: "LastModifiedAt",
            schema: "form",
            table: "FormSubmission");
    }
}

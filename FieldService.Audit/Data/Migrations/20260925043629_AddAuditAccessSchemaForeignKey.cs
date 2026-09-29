using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiledService.Audit.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAccessSchemaForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("LOCK TABLE \"AuditAccesses\" IN ACCESS EXCLUSIVE MODE;");
            migrationBuilder.Sql("LOCK TABLE \"AuditDtoSchemas\" IN ACCESS EXCLUSIVE MODE;");

            migrationBuilder.Sql(
                """
                INSERT INTO "AuditDtoSchemas" ("ResourceName", "Version", "CreatedAt", "Properties")
                SELECT DISTINCT a."ResourceName", a."SchemaVersion", NOW(), '{}'::jsonb
                FROM "AuditAccesses" a
                LEFT JOIN "AuditDtoSchemas" s
                    ON s."ResourceName" = a."ResourceName"
                   AND s."Version" = a."SchemaVersion"
                WHERE a."ResourceName" IS NOT NULL
                  AND a."SchemaVersion" IS NOT NULL
                  AND s."ResourceName" IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_AuditAccesses_AuditDtoSchemas_ResourceName_SchemaVersion",
                table: "AuditAccesses",
                columns: new[] { "ResourceName", "SchemaVersion" },
                principalTable: "AuditDtoSchemas",
                principalColumns: new[] { "ResourceName", "Version" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_AuditAccesses_AuditDtoSchemas_ResourceName_SchemaVersion",
                table: "AuditAccesses");
        }
    }
}

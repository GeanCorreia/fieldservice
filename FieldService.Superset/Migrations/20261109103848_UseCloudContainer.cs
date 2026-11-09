using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Superset.Migrations
{
    /// <inheritdoc />
    public partial class UseCloudContainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "Container"
                    ("Id", "TenantId", "SecretKeyId", "CloudProvider", "Name", "ResourceId",
                     "FqdnUrl", "ExecutionType", "MaxReplicas", "MinReplicas", "Status", "ExecutionWindow")
                SELECT "Id", "TenantId", "SecretKeyId", "ProviderType",
                       'superset_' || replace("TenantId"::text, '-', ''), "ResourceId",
                       "FqdnUrl", "ExecutionType", "MaxReplicas", "MinReplicas", "Status", "ExecutionWindow"
                FROM "SupersetContainer";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_SupersetTenant_SupersetContainer_ContainerId",
                table: "SupersetTenant");

            migrationBuilder.DropTable(
                name: "SupersetContainer");

            migrationBuilder.AddForeignKey(
                name: "FK_SupersetTenant_Container_ContainerId",
                table: "SupersetTenant",
                column: "ContainerId",
                principalTable: "Container",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupersetTenant_Container_ContainerId",
                table: "SupersetTenant");

            migrationBuilder.CreateTable(
                name: "SupersetContainer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionType = table.Column<int>(type: "integer", nullable: false),
                    FqdnUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxReplicas = table.Column<int>(type: "integer", nullable: false),
                    MinReplicas = table.Column<int>(type: "integer", nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SecretKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionWindow = table.Column<JsonElement>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupersetContainer", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupersetContainer_ResourceId",
                table: "SupersetContainer",
                column: "ResourceId",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "SupersetContainer"
                    ("Id", "TenantId", "SecretKeyId", "ProviderType", "ResourceId",
                     "FqdnUrl", "ExecutionType", "MaxReplicas", "MinReplicas", "Status", "ExecutionWindow")
                SELECT "Id", "TenantId", "SecretKeyId", "CloudProvider", "ResourceId",
                       "FqdnUrl", "ExecutionType", "MaxReplicas", "MinReplicas", "Status", "ExecutionWindow"
                FROM "Container"
                WHERE "Name" = 'superset_' || replace("TenantId"::text, '-', '');
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_SupersetTenant_SupersetContainer_ContainerId",
                table: "SupersetTenant",
                column: "ContainerId",
                principalTable: "SupersetContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                DELETE FROM "Container"
                WHERE "Name" = 'superset_' || replace("TenantId"::text, '-', '');
                """);
        }
    }
}

using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Superset.Migrations
{
    /// <inheritdoc />
    public partial class InitialSuperset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupersetContainer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    FqdnUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExecutionType = table.Column<int>(type: "integer", nullable: false),
                    MaxReplicas = table.Column<int>(type: "integer", nullable: false),
                    MinReplicas = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExecutionWindow = table.Column<JsonElement>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupersetContainer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupersetTenantFlow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Configuration = table.Column<string>(type: "jsonb", nullable: false),
                    CustomHostConnectionStringId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ContainerCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SecretKeyCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SecretKeyId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataSchemaCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConnectionStringId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersistedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupersetTenantFlow", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupersetTenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionStringId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupersetTenant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupersetTenant_SupersetContainer_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "SupersetContainer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupersetContainer_ResourceId",
                table: "SupersetContainer",
                column: "ResourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupersetTenant_ContainerId",
                table: "SupersetTenant",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupersetTenant_TenantId",
                table: "SupersetTenant",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupersetTenantFlow_TenantId",
                table: "SupersetTenantFlow",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupersetTenant");

            migrationBuilder.DropTable(
                name: "SupersetTenantFlow");

            migrationBuilder.DropTable(
                name: "SupersetContainer");
        }
    }
}

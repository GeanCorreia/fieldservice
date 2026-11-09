using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Cloud.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudContainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Container",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CloudProvider = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
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
                    table.PrimaryKey("PK_Container", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Container_ResourceId",
                table: "Container",
                column: "ResourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Container_TenantId_Name",
                table: "Container",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Container");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Authorization.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermissionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionModule = table.Column<string>(type: "text", nullable: false),
                    PermissionResource = table.Column<string>(type: "text", nullable: false),
                    PermissionAction = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSuspensionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OriginalSuspensionEventId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSuspensionEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_PermissionEvents_CreatedAt",
                table: "PermissionEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_PermissionEvents_Type",
                table: "PermissionEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "idx_PermissionEvents_UserId_TenantId",
                table: "PermissionEvents",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "idx_RoleEvents_CreatedAt",
                table: "RoleEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_RoleEvents_UserId_TenantId",
                table: "RoleEvents",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "idx_UserSuspensionEvents_CreatedAt",
                table: "UserSuspensionEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_UserSuspensionEvents_OriginalSuspensionEventId",
                table: "UserSuspensionEvents",
                column: "OriginalSuspensionEventId");

            migrationBuilder.CreateIndex(
                name: "idx_UserSuspensionEvents_Type",
                table: "UserSuspensionEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "idx_UserSuspensionEvents_UserId_TenantId",
                table: "UserSuspensionEvents",
                columns: new[] { "UserId", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermissionEvents");

            migrationBuilder.DropTable(
                name: "RoleEvents");

            migrationBuilder.DropTable(
                name: "UserSuspensionEvents");
        }
    }
}

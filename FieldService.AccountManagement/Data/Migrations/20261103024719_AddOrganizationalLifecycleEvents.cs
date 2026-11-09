using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.AccountManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationalLifecycleEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TenantEvents_Tenant_Team_Deactivated_CreatedAt",
                table: "TenantEvents",
                columns: new[] { "TenantId", "TeamId", "CreatedAt" },
                filter: "\"EventType\" = 17");

            migrationBuilder.CreateIndex(
                name: "IX_TenantEvents_Tenant_User_TeamTransfer_CreatedAt",
                table: "TenantEvents",
                columns: new[] { "TenantId", "UserId", "CreatedAt" },
                filter: "\"EventType\" = 16");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantEvents_Tenant_Team_Deactivated_CreatedAt",
                table: "TenantEvents");

            migrationBuilder.DropIndex(
                name: "IX_TenantEvents_Tenant_User_TeamTransfer_CreatedAt",
                table: "TenantEvents");
        }
    }
}

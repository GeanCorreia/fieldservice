using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.AccountManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegisteredEventUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_TenantEvents_Tenant_JobTitle_Registered",
                table: "TenantEvents",
                columns: new[] { "TenantId", "JobTitleId" },
                unique: true,
                filter: "\"EventType\" = 6");

            migrationBuilder.CreateIndex(
                name: "UX_TenantEvents_Tenant_Registered",
                table: "TenantEvents",
                column: "TenantId",
                unique: true,
                filter: "\"EventType\" = 1");

            migrationBuilder.CreateIndex(
                name: "UX_TenantEvents_Tenant_Team_Registered",
                table: "TenantEvents",
                columns: new[] { "TenantId", "TeamId" },
                unique: true,
                filter: "\"EventType\" = 14");

            migrationBuilder.CreateIndex(
                name: "UX_TenantEvents_Tenant_User_PersonalInfoRegistered",
                table: "TenantEvents",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "\"EventType\" = 15");

            migrationBuilder.CreateIndex(
                name: "UX_TenantEvents_Tenant_User_Registered",
                table: "TenantEvents",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "\"EventType\" = 13");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TenantEvents_Tenant_JobTitle_Registered",
                table: "TenantEvents");

            migrationBuilder.DropIndex(
                name: "UX_TenantEvents_Tenant_Registered",
                table: "TenantEvents");

            migrationBuilder.DropIndex(
                name: "UX_TenantEvents_Tenant_Team_Registered",
                table: "TenantEvents");

            migrationBuilder.DropIndex(
                name: "UX_TenantEvents_Tenant_User_PersonalInfoRegistered",
                table: "TenantEvents");

            migrationBuilder.DropIndex(
                name: "UX_TenantEvents_Tenant_User_Registered",
                table: "TenantEvents");
        }
    }
}

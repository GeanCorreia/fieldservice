using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.AccountManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateAccountManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    JobTitleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantEvents_Tenant_JobTitle_Type",
                table: "TenantEvents",
                columns: new[] { "TenantId", "JobTitleId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantEvents_Tenant_Team_Type",
                table: "TenantEvents",
                columns: new[] { "TenantId", "TeamId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantEvents_Tenant_Type_CreatedAt",
                table: "TenantEvents",
                columns: new[] { "TenantId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantEvents_Tenant_User_Type",
                table: "TenantEvents",
                columns: new[] { "TenantId", "UserId", "EventType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantEvents");
        }
    }
}

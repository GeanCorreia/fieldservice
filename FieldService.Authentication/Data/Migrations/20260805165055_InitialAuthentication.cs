using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Authentication.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Session",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<int>(type: "integer", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthentication",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthentication", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "SessionActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    JwtId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IpAddressHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserAgentHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionActivity_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTenantEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTenantEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTenantEvent_UserAuthentication_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAuthentication",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_Session_ExpiresAt",
                table: "Session",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "idx_Session_UserId_TenantId",
                table: "Session",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "idx_SessionActivity_SessionId",
                table: "SessionActivity",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "idx_SessionActivity_Timestamp",
                table: "SessionActivity",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_UserAuthentication_ExternalId_Provider",
                table: "UserAuthentication",
                columns: new[] { "ExternalId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_UserTenantEvent_UserId",
                table: "UserTenantEvent",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_UserTenantEvent_UserId_TenantId_CreatedAt",
                table: "UserTenantEvent",
                columns: new[] { "UserId", "TenantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionActivity");

            migrationBuilder.DropTable(
                name: "UserTenantEvent");

            migrationBuilder.DropTable(
                name: "Session");

            migrationBuilder.DropTable(
                name: "UserAuthentication");
        }
    }
}

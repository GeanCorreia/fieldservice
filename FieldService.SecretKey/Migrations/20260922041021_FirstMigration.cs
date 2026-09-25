using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.SecretKey.Migrations
{
    /// <inheritdoc />
    public partial class FirstMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecretKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecretKeyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SecretKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TypeName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretKeyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecretKeyEvents_SecretKeys_SecretKeyId",
                        column: x => x.SecretKeyId,
                        principalTable: "SecretKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecretKeyEvents_SecretKeyId",
                table: "SecretKeyEvents",
                column: "SecretKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretKeyEvents_SecretKeyId_OccurredAt",
                table: "SecretKeyEvents",
                columns: new[] { "SecretKeyId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecretKeys_TenantId",
                table: "SecretKeys",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretKeys_TenantId_Type",
                table: "SecretKeys",
                columns: new[] { "TenantId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecretKeyEvents");

            migrationBuilder.DropTable(
                name: "SecretKeys");
        }
    }
}

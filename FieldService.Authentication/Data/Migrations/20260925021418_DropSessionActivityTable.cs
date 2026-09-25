using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Authentication.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropSessionActivityTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"SessionActivity\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    IpAddressHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    JwtId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Resource = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    Successful = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserAgentHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionActivity", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_SessionActivity_SessionId",
                table: "SessionActivity",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "idx_SessionActivity_Timestamp",
                table: "SessionActivity",
                column: "Timestamp");
        }
    }
}

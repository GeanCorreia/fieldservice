using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Authentication.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncAuthenticationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionActivity_Session_SessionId",
                table: "SessionActivity");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "Session");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastActivityAt",
                table: "Session",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionActivity_Session_SessionId",
                table: "SessionActivity",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

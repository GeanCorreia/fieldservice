using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Authentication.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefatoracaoSessionActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgentHash",
                table: "SessionActivity",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Resource",
                table: "SessionActivity",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "SessionActivity",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Successful",
                table: "SessionActivity",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resource",
                table: "SessionActivity");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "SessionActivity");

            migrationBuilder.DropColumn(
                name: "Successful",
                table: "SessionActivity");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgentHash",
                table: "SessionActivity",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);
        }
    }
}

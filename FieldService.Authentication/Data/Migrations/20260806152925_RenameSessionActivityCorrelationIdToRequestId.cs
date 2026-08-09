using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Authentication.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameSessionActivityCorrelationIdToRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CorrelationId",
                table: "SessionActivity",
                newName: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestId",
                table: "SessionActivity",
                newName: "CorrelationId");
        }
    }
}

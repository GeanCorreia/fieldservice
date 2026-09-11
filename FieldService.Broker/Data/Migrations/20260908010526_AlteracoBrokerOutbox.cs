using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Broker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlteracoBrokerOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishContext",
                table: "BrokerOutbox");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<JsonDocument>(
                name: "PublishContext",
                table: "BrokerOutbox",
                type: "jsonb",
                nullable: false);
        }
    }
}

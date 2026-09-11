using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Broker.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialBroker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrokerOutbox",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Message = table.Column<JsonElement>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerOutbox", x => x.MessageId);
                });

            migrationBuilder.CreateIndex(
                name: "idx_BrokerOutbox_CreatedAt",
                table: "BrokerOutbox",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_BrokerOutbox_DispatchedAt",
                table: "BrokerOutbox",
                column: "DispatchedAt");

            migrationBuilder.CreateIndex(
                name: "idx_BrokerOutbox_ExpiresAt",
                table: "BrokerOutbox",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrokerOutbox");
        }
    }
}

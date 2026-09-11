using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Broker.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefatorarBrokerOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                     ALTER TABLE "BrokerOutbox"
                                     ADD COLUMN "PublishContext" jsonb NOT NULL DEFAULT '{}'::jsonb;
                                 """);

            migrationBuilder.Sql("""
                                     ALTER TABLE "BrokerOutbox"
                                     ALTER COLUMN "PublishContext" DROP DEFAULT;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishContext",
                table: "BrokerOutbox");
        }
    }
}

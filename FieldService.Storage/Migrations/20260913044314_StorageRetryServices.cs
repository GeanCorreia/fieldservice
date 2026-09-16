using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldService.Storage.Migrations
{
    /// <inheritdoc />
    public partial class StorageRetryServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredFileCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    MaxSizeInBytes = table.Column<long>(type: "bigint", nullable: true),
                    AllowedContentTypes = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    MinimumRequiredRole = table.Column<int>(type: "integer", nullable: true),
                    AllowedPermissions = table.Column<JsonElement>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFileCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StatusUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HashMd5 = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoredFiles_StoredFileCategories_FileCategoryId",
                        column: x => x.FileCategoryId,
                        principalTable: "StoredFileCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoredFileCategories_TenantId",
                table: "StoredFileCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFileCategories_TenantId_Code",
                table: "StoredFileCategories",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_FileCategoryId",
                table: "StoredFiles",
                column: "FileCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_Status",
                table: "StoredFiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_StoragePath",
                table: "StoredFiles",
                column: "StoragePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_UploadedByUserId",
                table: "StoredFiles",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoredFiles");

            migrationBuilder.DropTable(
                name: "StoredFileCategories");
        }
    }
}

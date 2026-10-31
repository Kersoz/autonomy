using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Autonomy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantRoleAssignments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d101"), "d23be2ec-37c4-48b0-80a6-b0d7b226d101", "PlatformAdmin", "PLATFORMADMIN" },
                    { new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d102"), "d23be2ec-37c4-48b0-80a6-b0d7b226d102", "FleetAdmin", "FLEETADMIN" },
                    { new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d103"), "d23be2ec-37c4-48b0-80a6-b0d7b226d103", "Dispatcher", "DISPATCHER" },
                    { new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d104"), "d23be2ec-37c4-48b0-80a6-b0d7b226d104", "MerchantUser", "MERCHANTUSER" },
                    { new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d105"), "d23be2ec-37c4-48b0-80a6-b0d7b226d105", "Courier", "COURIER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAtUtc",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_UserId_TenantType_TenantId_RoleName",
                table: "TenantRoleAssignments",
                columns: new[] { "UserId", "TenantType", "TenantId", "RoleName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "TenantRoleAssignments");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d101"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d102"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d103"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d104"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("d23be2ec-37c4-48b0-80a6-b0d7b226d105"));
        }
    }
}

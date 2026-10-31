using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autonomy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FleetTenantAuditInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_UserId_TenantType_TenantId_RoleName",
                table: "TenantRoleAssignments");

            migrationBuilder.AddColumn<Guid>(
                name: "MerchantId",
                table: "TenantRoleAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MerchantId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Merchants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Merchants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Merchants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "JobOffers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "JobOffers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "JobOffers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Fleets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Fleets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Fleets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "FleetPolicies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "FleetPolicies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "FleetPolicies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "DeliveryStatusHistories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "DeliveryStatusHistories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "DeliveryStatusHistories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Deliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Deliveries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Deliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Couriers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Couriers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Couriers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "CourierLocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CourierLocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "CourierLocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "CourierAppInstallations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CourierAppInstallations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "CourierAppInstallations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Fleets" SET "TenantId" = "Id";
                UPDATE "Merchants" SET "TenantId" = "FleetId";
                UPDATE "Couriers" SET "TenantId" = "FleetId";
                UPDATE "CourierAppInstallations" SET "TenantId" = "FleetId";
                UPDATE "CourierLocations" SET "TenantId" = "FleetId";
                UPDATE "Deliveries" SET "TenantId" = "FleetId";
                UPDATE "Assignments" SET "TenantId" = "FleetId";
                UPDATE "JobOffers" SET "TenantId" = "FleetId";
                UPDATE "FleetPolicies" SET "TenantId" = "FleetId";
                UPDATE "DeliveryStatusHistories" AS history
                SET "TenantId" = delivery."FleetId"
                FROM "Deliveries" AS delivery
                WHERE history."DeliveryId" = delivery."Id";

                UPDATE "TenantRoleAssignments" AS assignment
                SET "MerchantId" = merchant."Id",
                    "TenantId" = merchant."FleetId"
                FROM "Merchants" AS merchant
                WHERE assignment."TenantType" = 'merchant'
                  AND merchant."Id" = assignment."TenantId";

                UPDATE "RefreshTokens" AS token
                SET "MerchantId" = merchant."Id",
                    "TenantId" = merchant."FleetId"
                FROM "Merchants" AS merchant
                WHERE token."TenantType" = 'merchant'
                  AND merchant."Id" = token."TenantId";

                UPDATE "RefreshTokens"
                SET "RevokedAtUtc" = COALESCE("RevokedAtUtc", NOW())
                WHERE "TenantType" = 'merchant' AND "MerchantId" IS NULL;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "TenantRoleAssignments"
                        WHERE "TenantType" = 'merchant' AND "MerchantId" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Tenant backfill failed for merchant role assignments.';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM "DeliveryStatusHistories"
                        WHERE "TenantId" = '00000000-0000-0000-0000-000000000000'
                    ) THEN
                        RAISE EXCEPTION 'Tenant backfill failed for delivery status history.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "TenantType",
                table: "TenantRoleAssignments");

            migrationBuilder.DropColumn(
                name: "TenantType",
                table: "RefreshTokens");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_TenantId",
                table: "TenantRoleAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_UserId_TenantId_MerchantId_RoleName",
                table: "TenantRoleAssignments",
                columns: new[] { "UserId", "TenantId", "MerchantId", "RoleName" },
                unique: true,
                filter: "\"MerchantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_UserId_TenantId_RoleName",
                table: "TenantRoleAssignments",
                columns: new[] { "UserId", "TenantId", "RoleName" },
                unique: true,
                filter: "\"MerchantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_TenantId",
                table: "Merchants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_TenantId",
                table: "JobOffers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_TenantId",
                table: "Fleets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetPolicies_TenantId",
                table: "FleetPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryStatusHistories_TenantId",
                table: "DeliveryStatusHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_TenantId",
                table: "Deliveries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Couriers_TenantId",
                table: "Couriers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierLocations_TenantId",
                table: "CourierLocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierAppInstallations_TenantId",
                table: "CourierAppInstallations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TenantId",
                table: "Assignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "TenantId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_TenantId",
                table: "TenantRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_UserId_TenantId_MerchantId_RoleName",
                table: "TenantRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_UserId_TenantId_RoleName",
                table: "TenantRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_TenantId",
                table: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_TenantId",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_Fleets_TenantId",
                table: "Fleets");

            migrationBuilder.DropIndex(
                name: "IX_FleetPolicies_TenantId",
                table: "FleetPolicies");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryStatusHistories_TenantId",
                table: "DeliveryStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_TenantId",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Couriers_TenantId",
                table: "Couriers");

            migrationBuilder.DropIndex(
                name: "IX_CourierLocations_TenantId",
                table: "CourierLocations");

            migrationBuilder.DropIndex(
                name: "IX_CourierAppInstallations_TenantId",
                table: "CourierAppInstallations");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TenantId",
                table: "Assignments");

            migrationBuilder.AddColumn<string>(
                name: "TenantType",
                table: "TenantRoleAssignments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "fleet");

            migrationBuilder.AddColumn<string>(
                name: "TenantType",
                table: "RefreshTokens",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "TenantRoleAssignments"
                SET "TenantType" = CASE WHEN "MerchantId" IS NULL THEN 'fleet' ELSE 'merchant' END,
                    "TenantId" = COALESCE("MerchantId", "TenantId");
                UPDATE "RefreshTokens"
                SET "TenantType" = CASE
                        WHEN "TenantId" IS NULL THEN NULL
                        WHEN "MerchantId" IS NULL THEN 'fleet'
                        ELSE 'merchant'
                    END,
                    "TenantId" = COALESCE("MerchantId", "TenantId");
                """);

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "TenantRoleAssignments");

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Fleets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Fleets");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Fleets");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FleetPolicies");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "FleetPolicies");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "FleetPolicies");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DeliveryStatusHistories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DeliveryStatusHistories");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DeliveryStatusHistories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Couriers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Couriers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Couriers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CourierLocations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CourierLocations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CourierLocations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CourierAppInstallations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CourierAppInstallations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CourierAppInstallations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_UserId_TenantType_TenantId_RoleName",
                table: "TenantRoleAssignments",
                columns: new[] { "UserId", "TenantType", "TenantId", "RoleName" },
                unique: true);
        }
    }
}

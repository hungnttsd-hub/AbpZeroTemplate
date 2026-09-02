using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebHoanTien.EntityFrameworkCore;

#nullable disable

namespace WebHoanTien.Migrations;

[DbContext(typeof(WebHoanTienDbContext))]
[Migration("20260902090000_AddOrderItemAttributions")]
public partial class AddOrderItemAttributions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Platform",
            schema: "affiliate",
            table: "Order",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.Sql("""
            UPDATE affiliate."Order" AS o
            SET "Platform" = c."Platform"
            FROM affiliate."Conversion" AS c
            WHERE c."Id" = o."ConversionId";
            """);

        migrationBuilder.Sql("""
            ALTER TABLE affiliate."Order" ALTER COLUMN "Platform" DROP DEFAULT;
            """);

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM affiliate."Order"
                    WHERE "IsDeleted" = FALSE
                    GROUP BY "Platform", "ExternalOrderId"
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'CatsBack migration stopped: duplicate affiliate orders exist for the same Platform/ExternalOrderId. Resolve financial duplicates before retrying.';
                END IF;
            END $$;
            """);

        migrationBuilder.DropIndex(
            name: "IX_Order_ConversionId_ExternalOrderId",
            schema: "affiliate",
            table: "Order");

        migrationBuilder.CreateIndex(
            name: "IX_Order_Platform_ExternalOrderId",
            schema: "affiliate",
            table: "Order",
            columns: new[] { "Platform", "ExternalOrderId" },
            unique: true,
            filter: "\"IsDeleted\" = FALSE");

        migrationBuilder.CreateIndex(
            name: "IX_Order_ConversionId",
            schema: "affiliate",
            table: "Order",
            column: "ConversionId");

        migrationBuilder.DropIndex(
            name: "IX_OrderItem_OrderId_ExternalItemId_ModelId",
            schema: "affiliate",
            table: "OrderItem");

        migrationBuilder.CreateIndex(
            name: "IX_OrderItem_OrderId_ExternalItemId_ModelId",
            schema: "affiliate",
            table: "OrderItem",
            columns: new[] { "OrderId", "ExternalItemId", "ModelId" },
            unique: true,
            filter: "\"IsDeleted\" = FALSE");

        migrationBuilder.CreateTable(
            name: "OrderItemAttribution",
            schema: "affiliate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                AttributionValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                TrackingId = table.Column<Guid>(type: "uuid", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                PurchaseAmount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                ItemTotalCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                AllocatedNetCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                UserShareRate = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                UserCommissionSnapshot = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                SettledNetCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: true),
                SettledUserCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: true),
                RefundAmount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                IsFraud = table.Column<bool>(type: "boolean", nullable: false),
                ProviderStatus = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderItemAttribution", x => x.Id);
                table.ForeignKey("FK_OrderItemAttribution_OrderItem_OrderItemId", x => x.OrderItemId,
                    principalSchema: "affiliate", principalTable: "OrderItem", principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_OrderItemAttribution_Tracking_TrackingId", x => x.TrackingId,
                    principalSchema: "affiliate", principalTable: "Tracking", principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_OrderItemAttribution_AbpUsers_UserId", x => x.UserId,
                    principalTable: "AbpUsers", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_OrderItemAttribution_OrderItemId_AttributionValue",
            schema: "affiliate", table: "OrderItemAttribution",
            columns: new[] { "OrderItemId", "AttributionValue" }, unique: true,
            filter: "\"IsDeleted\" = FALSE");
        migrationBuilder.CreateIndex("IX_OrderItemAttribution_TrackingId", schema: "affiliate",
            table: "OrderItemAttribution", column: "TrackingId");
        migrationBuilder.CreateIndex("IX_OrderItemAttribution_UserId_OrderItemId", schema: "affiliate",
            table: "OrderItemAttribution", columns: new[] { "UserId", "OrderItemId" });

        migrationBuilder.Sql("""
            WITH base AS (
                SELECT i.*, o."SettledNetCommission" AS order_settled_net,
                       o."SettledUserCommission" AS order_settled_user,
                       c."TrackingId", c."UserId", c."AttributionValue", c."UserShareRate",
                       ROW_NUMBER() OVER (PARTITION BY i."OrderId" ORDER BY i."Id") AS row_number,
                       COUNT(*) OVER (PARTITION BY i."OrderId") AS row_count,
                       CASE WHEN o."SettledNetCommission" IS NULL THEN NULL
                            WHEN o."NetCommission" = 0 THEN 0
                            ELSE ROUND(o."SettledNetCommission" * i."AllocatedNetCommission" / o."NetCommission", 4) END AS provisional_net,
                       CASE WHEN o."SettledUserCommission" IS NULL THEN NULL
                            WHEN o."UserCommissionSnapshot" = 0 THEN 0
                            ELSE ROUND(o."SettledUserCommission" * i."UserCommissionSnapshot" / o."UserCommissionSnapshot", 4) END AS provisional_user
                FROM affiliate."OrderItem" i
                JOIN affiliate."Order" o ON o."Id" = i."OrderId"
                JOIN affiliate."Conversion" c ON c."Id" = o."ConversionId"
                WHERE i."IsDeleted" = FALSE AND o."IsDeleted" = FALSE AND c."IsDeleted" = FALSE
            ), allocated AS (
                SELECT base.*,
                       CASE WHEN row_number = row_count AND order_settled_net IS NOT NULL
                            THEN order_settled_net - SUM(provisional_net) OVER (PARTITION BY "OrderId") + provisional_net
                            ELSE provisional_net END AS settled_net,
                       CASE WHEN row_number = row_count AND order_settled_user IS NOT NULL
                            THEN order_settled_user - SUM(provisional_user) OVER (PARTITION BY "OrderId") + provisional_user
                            ELSE provisional_user END AS settled_user
                FROM base
            )
            INSERT INTO affiliate."OrderItemAttribution" (
                "Id", "OrderItemId", "AttributionValue", "TrackingId", "UserId", "Status",
                "PurchaseAmount", "Quantity", "ItemTotalCommission", "AllocatedNetCommission",
                "UserShareRate", "UserCommissionSnapshot", "SettledNetCommission", "SettledUserCommission",
                "RefundAmount", "IsFraud", "ProviderStatus", "CreationTime", "CreatorId",
                "LastModificationTime", "LastModifierId", "IsDeleted")
            SELECT "Id", "Id", COALESCE(NULLIF("AttributionValue", ''), 'legacy-conversion:' || "OrderId"::text),
                   "TrackingId", "UserId", CASE WHEN "TrackingId" IS NOT NULL AND "UserId" IS NOT NULL THEN 1 ELSE 0 END,
                   "PurchaseAmount", "Quantity", "ItemTotalCommission", "AllocatedNetCommission",
                   CASE WHEN "UserId" IS NULL THEN 0 ELSE "UserShareRate" END,
                   CASE WHEN "UserId" IS NULL THEN 0 ELSE "UserCommissionSnapshot" END,
                   settled_net, settled_user, "RefundAmount", "IsFraud", "ProviderStatus",
                   "CreationTime", "CreatorId", "LastModificationTime", "LastModifierId", FALSE
            FROM allocated;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OrderItemAttribution", schema: "affiliate");
        migrationBuilder.DropIndex(name: "IX_OrderItem_OrderId_ExternalItemId_ModelId",
            schema: "affiliate", table: "OrderItem");
        migrationBuilder.CreateIndex(name: "IX_OrderItem_OrderId_ExternalItemId_ModelId",
            schema: "affiliate", table: "OrderItem",
            columns: new[] { "OrderId", "ExternalItemId", "ModelId" }, unique: true);
        migrationBuilder.DropIndex(name: "IX_Order_ConversionId", schema: "affiliate", table: "Order");
        migrationBuilder.DropIndex(name: "IX_Order_Platform_ExternalOrderId", schema: "affiliate", table: "Order");
        migrationBuilder.CreateIndex(name: "IX_Order_ConversionId_ExternalOrderId", schema: "affiliate",
            table: "Order", columns: new[] { "ConversionId", "ExternalOrderId" }, unique: true);
        migrationBuilder.DropColumn(name: "Platform", schema: "affiliate", table: "Order");
    }
}

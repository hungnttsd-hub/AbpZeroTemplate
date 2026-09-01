using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebHoanTien.EntityFrameworkCore;

#nullable disable

namespace WebHoanTien.Migrations;

[DbContext(typeof(WebHoanTienDbContext))]
[Migration("20260901090000_AddShopeeSettlementProviderStatus")]
public partial class AddShopeeSettlementProviderStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "WaitingPaymentCount",
            schema: "affiliate",
            table: "ShopeeSettlementBatch",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<DateTime>(
            name: "PaidAt",
            schema: "affiliate",
            table: "ShopeeSettlementBill",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AddColumn<int>(name: "PaymentStatus", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "integer", nullable: false, defaultValue: 4);
        migrationBuilder.AddColumn<int>(name: "ValidationPayoutStatus", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "integer", nullable: false, defaultValue: 2);
        migrationBuilder.AddColumn<int>(name: "OverallValidationStatus", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "BillValidationStatus", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "SettlementCycle", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "integer", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "HasAdjustment", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "HasClawback", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "IsCumulative", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "HasBonus", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "HasPpp", table: "ShopeeSettlementBill", schema: "affiliate",
            type: "boolean", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("WaitingPaymentCount", "ShopeeSettlementBatch", "affiliate");
        migrationBuilder.DropColumn("PaymentStatus", "ShopeeSettlementBill", "affiliate");
        migrationBuilder.DropColumn("ValidationPayoutStatus", "ShopeeSettlementBill", "affiliate");
        migrationBuilder.DropColumn("OverallValidationStatus", "ShopeeSettlementBill", "affiliate");
        migrationBuilder.DropColumn("BillValidationStatus", "ShopeeSettlementBill", "affiliate");
        migrationBuilder.DropColumn("SettlementCycle", "ShopeeSettlementBill", "affiliate");
        migrationBuilder.DropColumn("HasAdjustment", "ShopeeSettlementBill", "affiliate");
        migrationBuilder.DropColumn("HasClawback", "ShopeeSettlementBill", "affiliate");
        migrationBuilder.DropColumn("IsCumulative", "ShopeeSettlementBill", "affiliate");
        migrationBuilder.DropColumn("HasBonus", "ShopeeSettlementBill", "affiliate");
        migrationBuilder.DropColumn("HasPpp", "ShopeeSettlementBill", "affiliate");

        migrationBuilder.AlterColumn<DateTime>(
            name: "PaidAt",
            schema: "affiliate",
            table: "ShopeeSettlementBill",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: DateTime.UnixEpoch,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);
    }
}

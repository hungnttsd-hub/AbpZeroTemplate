using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHoanTien.Migrations
{
    /// <inheritdoc />
    public partial class AddShopeeOrderSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAt",
                schema: "affiliate",
                table: "Order",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SettledNetCommission",
                schema: "affiliate",
                table: "Order",
                type: "numeric(20,4)",
                precision: 20,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SettledUserCommission",
                schema: "affiliate",
                table: "Order",
                type: "numeric(20,4)",
                precision: 20,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementReference",
                schema: "affiliate",
                table: "Order",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Order_Status_SettledAt",
                schema: "affiliate",
                table: "Order",
                columns: new[] { "Status", "SettledAt" });

            migrationBuilder.Sql("UPDATE affiliate.\"Order\" SET \"PayableUserCommission\" = 0 WHERE \"Status\" IN (1, 2, 3);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE affiliate.\"Order\" SET \"PayableUserCommission\" = \"UserCommissionSnapshot\" WHERE \"Status\" = 3;");

            migrationBuilder.DropIndex(
                name: "IX_Order_Status_SettledAt",
                schema: "affiliate",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "SettledAt",
                schema: "affiliate",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "SettledNetCommission",
                schema: "affiliate",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "SettledUserCommission",
                schema: "affiliate",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "SettlementReference",
                schema: "affiliate",
                table: "Order");
        }
    }
}

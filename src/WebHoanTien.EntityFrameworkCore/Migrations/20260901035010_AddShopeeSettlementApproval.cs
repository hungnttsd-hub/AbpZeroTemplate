using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHoanTien.Migrations;

public partial class AddShopeeSettlementApproval : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ShopeeSettlementBatch",
            schema: "affiliate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Source = table.Column<int>(type: "integer", nullable: false),
                OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                BillCount = table.Column<int>(type: "integer", nullable: false),
                RecordCount = table.Column<int>(type: "integer", nullable: false),
                PendingCount = table.Column<int>(type: "integer", nullable: false),
                ApprovedCount = table.Column<int>(type: "integer", nullable: false),
                UnmatchedCount = table.Column<int>(type: "integer", nullable: false),
                AlreadySettledCount = table.Column<int>(type: "integer", nullable: false),
                InvalidCount = table.Column<int>(type: "integer", nullable: false),
                TotalEligibleCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                TotalPaidCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                PendingPaidCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                ApprovedPaidCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                ExtraProperties = table.Column<string>(type: "text", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ShopeeSettlementBatch", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ShopeeSettlementBill",
            schema: "affiliate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceAffiliateId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ValidationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PayoutId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                OrderCompletedFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                OrderCompletedTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                EligibleCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                AfterServiceFeeCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                PaidCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                ServiceFeeAmount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                TaxAmount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                HasAuthoritativeEligibleCommission = table.Column<bool>(type: "boolean", nullable: false),
                RecordCount = table.Column<int>(type: "integer", nullable: false),
                ExtraProperties = table.Column<string>(type: "text", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShopeeSettlementBill", x => x.Id);
                table.ForeignKey(
                    name: "FK_ShopeeSettlementBill_ShopeeSettlementBatch_BatchId",
                    column: x => x.BatchId,
                    principalSchema: "affiliate",
                    principalTable: "ShopeeSettlementBatch",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ShopeeSettlementRecord",
            schema: "affiliate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                BillId = table.Column<Guid>(type: "uuid", nullable: false),
                ExternalOrderId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                EligibleCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                AllocatedServiceFee = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                AllocatedTax = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                ActualPaidCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                ApprovedUserCommission = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                AffiliateOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                AffiliateConversionId = table.Column<Guid>(type: "uuid", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                Issue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ExtraProperties = table.Column<string>(type: "text", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShopeeSettlementRecord", x => x.Id);
                table.ForeignKey("FK_ShopeeSettlementRecord_AbpUsers_ApprovedByUserId", x => x.ApprovedByUserId,
                    "AbpUsers", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_ShopeeSettlementRecord_AbpUsers_UserId", x => x.UserId,
                    "AbpUsers", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_ShopeeSettlementRecord_Conversion_AffiliateConversionId", x => x.AffiliateConversionId,
                    principalSchema: "affiliate", principalTable: "Conversion", principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_ShopeeSettlementRecord_Order_AffiliateOrderId", x => x.AffiliateOrderId,
                    principalSchema: "affiliate", principalTable: "Order", principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_ShopeeSettlementRecord_ShopeeSettlementBatch_BatchId", x => x.BatchId,
                    principalSchema: "affiliate", principalTable: "ShopeeSettlementBatch", principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ShopeeSettlementRecord_ShopeeSettlementBill_BillId", x => x.BillId,
                    principalSchema: "affiliate", principalTable: "ShopeeSettlementBill", principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_ShopeeSettlementBatch_ContentHash", "ShopeeSettlementBatch", "ContentHash",
            schema: "affiliate", unique: true);
        migrationBuilder.CreateIndex("IX_ShopeeSettlementBatch_Status_CreationTime", "ShopeeSettlementBatch",
            new[] { "Status", "CreationTime" }, schema: "affiliate");
        migrationBuilder.CreateIndex("IX_ShopeeSettlementBill_BatchId", "ShopeeSettlementBill", "BatchId",
            schema: "affiliate");
        migrationBuilder.CreateIndex("IX_ShopeeSettlementBill_PayoutId", "ShopeeSettlementBill", "PayoutId",
            schema: "affiliate");
        migrationBuilder.CreateIndex("IX_ShopeeSettlementBill_SourceAffiliateId_ValidationId", "ShopeeSettlementBill",
            new[] { "SourceAffiliateId", "ValidationId" }, schema: "affiliate", unique: true);
        migrationBuilder.CreateIndex("IX_ShopeeSettlementRecord_AffiliateConversionId", "ShopeeSettlementRecord",
            "AffiliateConversionId", schema: "affiliate");
        migrationBuilder.CreateIndex("IX_ShopeeSettlementRecord_AffiliateOrderId", "ShopeeSettlementRecord",
            "AffiliateOrderId", schema: "affiliate");
        migrationBuilder.CreateIndex("IX_ShopeeSettlementRecord_ApprovedByUserId", "ShopeeSettlementRecord",
            "ApprovedByUserId", schema: "affiliate");
        migrationBuilder.CreateIndex("IX_ShopeeSettlementRecord_BatchId_Status", "ShopeeSettlementRecord",
            new[] { "BatchId", "Status" }, schema: "affiliate");
        migrationBuilder.CreateIndex("IX_ShopeeSettlementRecord_BillId", "ShopeeSettlementRecord", "BillId",
            schema: "affiliate");
        migrationBuilder.CreateIndex("IX_ShopeeSettlementRecord_ExternalOrderId", "ShopeeSettlementRecord",
            "ExternalOrderId", schema: "affiliate", unique: true);
        migrationBuilder.CreateIndex("IX_ShopeeSettlementRecord_UserId", "ShopeeSettlementRecord", "UserId",
            schema: "affiliate");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ShopeeSettlementRecord", schema: "affiliate");
        migrationBuilder.DropTable(name: "ShopeeSettlementBill", schema: "affiliate");
        migrationBuilder.DropTable(name: "ShopeeSettlementBatch", schema: "affiliate");
    }
}

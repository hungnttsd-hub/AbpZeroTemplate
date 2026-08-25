using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebHoanTien.EntityFrameworkCore;

#nullable disable

namespace WebHoanTien.Migrations;

[DbContext(typeof(WebHoanTienDbContext))]
[Migration("20260823160000_AddWalletWithdrawals")]
public partial class AddWalletWithdrawals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WithdrawalRequest",
            schema: "affiliate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                RequestCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                PayoutAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                FeeAmount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                NetAmount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                BankCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AccountNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                AccountHolderName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                PaymentReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                AdminNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                ExtraProperties = table.Column<string>(type: "text", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
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
                table.PrimaryKey("PK_WithdrawalRequest", x => x.Id);
                table.ForeignKey("FK_WithdrawalRequest_AbpUsers_ProcessedByUserId", x => x.ProcessedByUserId,
                    principalTable: "AbpUsers", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_WithdrawalRequest_AbpUsers_UserId", x => x.UserId,
                    principalTable: "AbpUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WithdrawalRequest_UserPayoutAccount_PayoutAccountId", x => x.PayoutAccountId,
                    principalSchema: "affiliate", principalTable: "UserPayoutAccount", principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "WithdrawalPaymentProof",
            schema: "affiliate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WithdrawalRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Size = table.Column<long>(type: "bigint", nullable: false),
                Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Content = table.Column<byte[]>(type: "bytea", nullable: false),
                ExtraProperties = table.Column<string>(type: "text", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WithdrawalPaymentProof", x => x.Id);
                table.ForeignKey("FK_WithdrawalPaymentProof_WithdrawalRequest_WithdrawalRequestId",
                    x => x.WithdrawalRequestId, principalSchema: "affiliate", principalTable: "WithdrawalRequest",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_WithdrawalPaymentProof_WithdrawalRequestId", schema: "affiliate",
            table: "WithdrawalPaymentProof", column: "WithdrawalRequestId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_WithdrawalRequest_PayoutAccountId", schema: "affiliate",
            table: "WithdrawalRequest", column: "PayoutAccountId");
        migrationBuilder.CreateIndex(name: "IX_WithdrawalRequest_ProcessedByUserId", schema: "affiliate",
            table: "WithdrawalRequest", column: "ProcessedByUserId");
        migrationBuilder.CreateIndex(name: "IX_WithdrawalRequest_RequestCode", schema: "affiliate",
            table: "WithdrawalRequest", column: "RequestCode", unique: true);
        migrationBuilder.CreateIndex(name: "IX_WithdrawalRequest_Status_CreationTime", schema: "affiliate",
            table: "WithdrawalRequest", columns: new[] { "Status", "CreationTime" });
        migrationBuilder.CreateIndex(name: "IX_WithdrawalRequest_UserId_CreationTime", schema: "affiliate",
            table: "WithdrawalRequest", columns: new[] { "UserId", "CreationTime" });
        migrationBuilder.CreateIndex(name: "IX_WithdrawalRequest_UserId", schema: "affiliate",
            table: "WithdrawalRequest", column: "UserId", unique: true,
            filter: "\"Status\" = 1 AND \"IsDeleted\" = FALSE");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("WithdrawalPaymentProof", "affiliate");
        migrationBuilder.DropTable("WithdrawalRequest", "affiliate");
    }
}

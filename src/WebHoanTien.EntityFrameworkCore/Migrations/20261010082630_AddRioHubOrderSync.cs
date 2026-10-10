using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHoanTien.Migrations
{
    /// <inheritdoc />
    public partial class AddRioHubOrderSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RioHubOrderSku",
                schema: "affiliate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrderId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SkuId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProductName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TraceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    EstimatedCommission = table.Column<decimal>(type: "numeric", nullable: false),
                    ActualCommission = table.Column<decimal>(type: "numeric", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    RefundedQuantity = table.Column<int>(type: "integer", nullable: false),
                    FullyRefunded = table.Column<bool>(type: "boolean", nullable: false),
                    SettlementStatus = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProviderUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FetchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProviderJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RioHubOrderSku", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RioHubSyncCursor",
                schema: "affiliate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InitialStartUnix = table.Column<long>(type: "bigint", nullable: false),
                    WatermarkUnix = table.Column<long>(type: "bigint", nullable: true),
                    LastSucceededAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFetchedCount = table.Column<int>(type: "integer", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RioHubSyncCursor", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RioHubOrderSku_CreatorUsername_OrderId_SkuId",
                schema: "affiliate",
                table: "RioHubOrderSku",
                columns: new[] { "CreatorUsername", "OrderId", "SkuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RioHubOrderSku_CreatorUsername_Status",
                schema: "affiliate",
                table: "RioHubOrderSku",
                columns: new[] { "CreatorUsername", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RioHubOrderSku_CreatorUsername_SubId",
                schema: "affiliate",
                table: "RioHubOrderSku",
                columns: new[] { "CreatorUsername", "SubId" });

            migrationBuilder.CreateIndex(
                name: "IX_RioHubSyncCursor_CreatorUsername",
                schema: "affiliate",
                table: "RioHubSyncCursor",
                column: "CreatorUsername",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RioHubOrderSku",
                schema: "affiliate");

            migrationBuilder.DropTable(
                name: "RioHubSyncCursor",
                schema: "affiliate");
        }
    }
}

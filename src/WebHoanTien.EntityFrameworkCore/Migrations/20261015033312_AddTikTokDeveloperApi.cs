using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHoanTien.Migrations
{
    /// <inheritdoc />
    public partial class AddTikTokDeveloperApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TikTokDeveloperAccounts",
                schema: "affiliate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OpenId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AccessTokenEncrypted = table.Column<string>(type: "text", nullable: false),
                    RefreshTokenEncrypted = table.Column<string>(type: "text", nullable: false),
                    GrantedScopesJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AccessExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefreshExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TokenVersion = table.Column<long>(type: "bigint", nullable: false),
                    AuthorizationEpoch = table.Column<long>(type: "bigint", nullable: false),
                    RefreshLeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefreshLeaseUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RateWindowUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RateCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TikTokDeveloperAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TikTokDeveloperApiKeys",
                schema: "affiliate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyPreview = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TikTokDeveloperApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TikTokDeveloperAuthStates",
                schema: "affiliate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StateHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AuthorizationEpoch = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TikTokDeveloperAuthStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TikTokDeveloperApiKeys_KeyHash",
                schema: "affiliate",
                table: "TikTokDeveloperApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TikTokDeveloperApiKeys_OwnerUserId_AppKey",
                schema: "affiliate",
                table: "TikTokDeveloperApiKeys",
                columns: new[] { "OwnerUserId", "AppKey" });

            migrationBuilder.CreateIndex(
                name: "IX_TikTokDeveloperAuthStates_ExpiresAtUtc",
                schema: "affiliate",
                table: "TikTokDeveloperAuthStates",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TikTokDeveloperAuthStates_OwnerUserId_AppKey",
                schema: "affiliate",
                table: "TikTokDeveloperAuthStates",
                columns: new[] { "OwnerUserId", "AppKey" });

            migrationBuilder.CreateIndex(
                name: "IX_TikTokDeveloperAuthStates_StateHash",
                schema: "affiliate",
                table: "TikTokDeveloperAuthStates",
                column: "StateHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TikTokDeveloperAccounts",
                schema: "affiliate");

            migrationBuilder.DropTable(
                name: "TikTokDeveloperApiKeys",
                schema: "affiliate");

            migrationBuilder.DropTable(
                name: "TikTokDeveloperAuthStates",
                schema: "affiliate");
        }
    }
}

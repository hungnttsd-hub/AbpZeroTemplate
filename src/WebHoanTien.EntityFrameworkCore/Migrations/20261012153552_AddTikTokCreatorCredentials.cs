using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHoanTien.Migrations
{
    /// <inheritdoc />
    public partial class AddTikTokCreatorCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TikTokCreatorAuthStates",
                schema: "affiliate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StateHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AppKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    InitiatedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TikTokCreatorAuthStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TikTokCreatorCredentials",
                schema: "affiliate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OpenId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatorUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AccessTokenEncrypted = table.Column<string>(type: "text", nullable: false),
                    RefreshTokenEncrypted = table.Column<string>(type: "text", nullable: false),
                    AccessTokenExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RefreshTokenExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GrantedScopesJson = table.Column<string>(type: "text", nullable: false),
                    UserType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    AuthorizedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenVersion = table.Column<long>(type: "bigint", nullable: false),
                    RefreshLockId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefreshLockUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastRefreshedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TikTokCreatorCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TikTokCreatorAuthStates_ExpiresAtUtc",
                schema: "affiliate",
                table: "TikTokCreatorAuthStates",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TikTokCreatorAuthStates_StateHash",
                schema: "affiliate",
                table: "TikTokCreatorAuthStates",
                column: "StateHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TikTokCreatorCredentials_AppKey",
                schema: "affiliate",
                table: "TikTokCreatorCredentials",
                column: "AppKey",
                unique: true,
                filter: "\"IsPrimary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_TikTokCreatorCredentials_AppKey_OpenId",
                schema: "affiliate",
                table: "TikTokCreatorCredentials",
                columns: new[] { "AppKey", "OpenId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TikTokCreatorAuthStates",
                schema: "affiliate");

            migrationBuilder.DropTable(
                name: "TikTokCreatorCredentials",
                schema: "affiliate");
        }
    }
}

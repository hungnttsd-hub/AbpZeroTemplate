using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHoanTien.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonymousAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                table: "AbpUsers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "LoginEmail",
                table: "AbpUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedLoginEmail",
                table: "AbpUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatBackAnonymousDevice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatBackAnonymousDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatBackAnonymousDevice_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatBackAnonymousRecovery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedCode = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatBackAnonymousRecovery", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatBackAnonymousRecovery_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatBackPendingAccountUpgrade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReturnUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    TermsVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrivacyVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatBackPendingAccountUpgrade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatBackPendingAccountUpgrade_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE "AbpUsers"
                SET "LoginEmail" = CASE
                    WHEN btrim("UserName") ~ '^[^[:space:]@]+@[^[:space:]@]+\.[^[:space:]@]+$' THEN btrim("UserName")
                    ELSE NULLIF(btrim("Email"), '') END;
                UPDATE "AbpUsers" SET "NormalizedLoginEmail" = upper("LoginEmail");
                DO $preflight$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "AbpUsers" WHERE "NormalizedLoginEmail" IS NOT NULL
                               GROUP BY "NormalizedLoginEmail" HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'CatBack migration stopped: duplicate login emails. Resolve ownership before retrying.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "AbpUsers" a JOIN "AbpUsers" b
                               ON a."NormalizedLoginEmail" = upper(btrim(b."Email")) AND a."Id" <> b."Id") THEN
                        RAISE EXCEPTION 'CatBack migration stopped: login/contact email belongs to different users. Resolve ownership before retrying.';
                    END IF;
                END $preflight$;
                CREATE TABLE "CatBackAccountRateLimit" (
                    "Key" varchar(256) PRIMARY KEY,
                    "Count" integer NOT NULL,
                    "ExpiresAt" timestamptz NOT NULL);
                CREATE INDEX "IX_CatBackAccountRateLimit_ExpiresAt" ON "CatBackAccountRateLimit" ("ExpiresAt");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_NormalizedLoginEmail",
                table: "AbpUsers",
                column: "NormalizedLoginEmail",
                unique: true,
                filter: "\"NormalizedLoginEmail\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CatBackAnonymousDevice_SecretHash",
                table: "CatBackAnonymousDevice",
                column: "SecretHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatBackAnonymousDevice_UserId",
                table: "CatBackAnonymousDevice",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CatBackAnonymousRecovery_CodeHash",
                table: "CatBackAnonymousRecovery",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatBackAnonymousRecovery_UserId",
                table: "CatBackAnonymousRecovery",
                column: "UserId",
                unique: true,
                filter: "\"RevokedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CatBackPendingAccountUpgrade_TokenHash",
                table: "CatBackPendingAccountUpgrade",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatBackPendingAccountUpgrade_UserId",
                table: "CatBackPendingAccountUpgrade",
                column: "UserId",
                unique: true,
                filter: "\"RevokedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                    IF EXISTS (SELECT 1 FROM "AbpUsers" WHERE "AccountType" = 0) THEN
                        RAISE EXCEPTION 'Cannot remove anonymous-account protection while anonymous users exist. Disable new creation instead.';
                    END IF;
                END $guard$;
                DROP TABLE "CatBackAccountRateLimit";
                """);
            migrationBuilder.DropTable(
                name: "CatBackAnonymousDevice");

            migrationBuilder.DropTable(
                name: "CatBackAnonymousRecovery");

            migrationBuilder.DropTable(
                name: "CatBackPendingAccountUpgrade");

            migrationBuilder.DropIndex(
                name: "IX_AbpUsers_NormalizedLoginEmail",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "LoginEmail",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedLoginEmail",
                table: "AbpUsers");
        }
    }
}

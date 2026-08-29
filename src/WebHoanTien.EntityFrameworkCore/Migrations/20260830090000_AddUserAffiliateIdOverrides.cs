using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebHoanTien.EntityFrameworkCore;

#nullable disable

namespace WebHoanTien.Migrations;

[DbContext(typeof(WebHoanTienDbContext))]
[Migration("20260830090000_AddUserAffiliateIdOverrides")]
public partial class AddUserAffiliateIdOverrides : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AffiliateIdSnapshot",
            schema: "affiliate",
            table: "Click",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "UserAffiliateIdOverrideId",
            schema: "affiliate",
            table: "Click",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "UserAffiliateIdOverride",
            schema: "affiliate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Platform = table.Column<int>(type: "integer", nullable: false),
                AffiliateId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                AdminNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                table.PrimaryKey("PK_UserAffiliateIdOverride", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserAffiliateIdOverride_AbpUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AbpUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Click_UserAffiliateIdOverrideId",
            schema: "affiliate",
            table: "Click",
            column: "UserAffiliateIdOverrideId");

        migrationBuilder.CreateIndex(
            name: "IX_UserAffiliateIdOverride_AffiliateId",
            schema: "affiliate",
            table: "UserAffiliateIdOverride",
            column: "AffiliateId");

        migrationBuilder.CreateIndex(
            name: "IX_UserAffiliateIdOverride_UserId_Platform",
            schema: "affiliate",
            table: "UserAffiliateIdOverride",
            columns: new[] { "UserId", "Platform" },
            unique: true,
            filter: "\"IsDeleted\" = FALSE");

        migrationBuilder.AddForeignKey(
            name: "FK_Click_UserAffiliateIdOverride_UserAffiliateIdOverrideId",
            schema: "affiliate",
            table: "Click",
            column: "UserAffiliateIdOverrideId",
            principalSchema: "affiliate",
            principalTable: "UserAffiliateIdOverride",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Click_UserAffiliateIdOverride_UserAffiliateIdOverrideId",
            schema: "affiliate",
            table: "Click");

        migrationBuilder.DropTable(
            name: "UserAffiliateIdOverride",
            schema: "affiliate");

        migrationBuilder.DropColumn(
            name: "AffiliateIdSnapshot",
            schema: "affiliate",
            table: "Click");

        migrationBuilder.DropColumn(
            name: "UserAffiliateIdOverrideId",
            schema: "affiliate",
            table: "Click");
    }
}

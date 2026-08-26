using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebHoanTien.EntityFrameworkCore;

#nullable disable

namespace WebHoanTien.Migrations;

[DbContext(typeof(WebHoanTienDbContext))]
[Migration("20260826190000_AddCustomerNotifications")]
public partial class AddCustomerNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "notification");

        migrationBuilder.CreateTable(
            name: "NotificationCampaign",
            schema: "notification",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Message = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false),
                ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Audience = table.Column<int>(type: "integer", nullable: false),
                TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                RecipientCount = table.Column<int>(type: "integer", nullable: false),
                PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExtraProperties = table.Column<string>(type: "text", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationCampaign", x => x.Id);
                table.ForeignKey("FK_NotificationCampaign_AbpUsers_TargetUserId", x => x.TargetUserId,
                    principalTable: "AbpUsers", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "CustomerNotification",
            schema: "notification",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                Kind = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Message = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false),
                ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                EventKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                IsRead = table.Column<bool>(type: "boolean", nullable: false),
                ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ExtraProperties = table.Column<string>(type: "text", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerNotification", x => x.Id);
                table.ForeignKey("FK_CustomerNotification_AbpUsers_UserId", x => x.UserId,
                    principalTable: "AbpUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_NotificationCampaign_PublishedAt", schema: "notification",
            table: "NotificationCampaign", column: "PublishedAt");
        migrationBuilder.CreateIndex(name: "IX_NotificationCampaign_TargetUserId", schema: "notification",
            table: "NotificationCampaign", column: "TargetUserId");
        migrationBuilder.CreateIndex(name: "IX_CustomerNotification_UserId_Category_CreationTime",
            schema: "notification", table: "CustomerNotification",
            columns: new[] { "UserId", "Category", "CreationTime" });
        migrationBuilder.CreateIndex(name: "IX_CustomerNotification_UserId_EventKey", schema: "notification",
            table: "CustomerNotification", columns: new[] { "UserId", "EventKey" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_CustomerNotification_UserId_IsRead_CreationTime",
            schema: "notification", table: "CustomerNotification",
            columns: new[] { "UserId", "IsRead", "CreationTime" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CustomerNotification", schema: "notification");
        migrationBuilder.DropTable(name: "NotificationCampaign", schema: "notification");
    }
}

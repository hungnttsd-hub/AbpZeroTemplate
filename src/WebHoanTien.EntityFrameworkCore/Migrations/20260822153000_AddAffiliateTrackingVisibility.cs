using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebHoanTien.EntityFrameworkCore.Migrations;

[DbContext(typeof(WebHoanTienDbContext))]
[Migration("20260822153000_AddAffiliateTrackingVisibility")]
public partial class AddAffiliateTrackingVisibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "HiddenAt",
            schema: "affiliate",
            table: "Tracking",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsHidden",
            schema: "affiliate",
            table: "Tracking",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "HiddenAt", schema: "affiliate", table: "Tracking");
        migrationBuilder.DropColumn(name: "IsHidden", schema: "affiliate", table: "Tracking");
    }
}

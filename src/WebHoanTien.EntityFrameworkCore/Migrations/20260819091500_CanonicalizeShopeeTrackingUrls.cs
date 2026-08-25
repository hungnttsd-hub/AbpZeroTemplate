using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebHoanTien.EntityFrameworkCore.Migrations;

[DbContext(typeof(WebHoanTienDbContext))]
[Migration("20260819091500_CanonicalizeShopeeTrackingUrls")]
public partial class CanonicalizeShopeeTrackingUrls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE TEMP TABLE ""CanonicalTracking"" ON COMMIT DROP AS
WITH canonicalized AS (
    SELECT
        ""Id"",
        ""UserId"",
        ""Platform"",
        ""Status"",
        ""IsDeleted"",
        ""ClickCount"",
        ""CreationTime"",
        CASE
            WHEN ""NormalizedUrl"" ~ '^https://[^/]*shopee\.vn/product/[0-9]+/[0-9]+'
                THEN 'https://shopee.vn/product/' ||
                     (regexp_match(""NormalizedUrl"", '^https://[^/]*shopee\.vn/product/([0-9]+)/([0-9]+)'))[1] || '/' ||
                     (regexp_match(""NormalizedUrl"", '^https://[^/]*shopee\.vn/product/([0-9]+)/([0-9]+)'))[2]
            WHEN ""NormalizedUrl"" ~ '-i\.[0-9]+\.[0-9]+'
                THEN 'https://shopee.vn/product/' ||
                     (regexp_match(""NormalizedUrl"", '-i\.([0-9]+)\.([0-9]+)'))[1] || '/' ||
                     (regexp_match(""NormalizedUrl"", '-i\.([0-9]+)\.([0-9]+)'))[2]
            ELSE ""NormalizedUrl""
        END AS ""CanonicalUrl""
    FROM affiliate.""Tracking""
), ranked AS (
    SELECT *, ROW_NUMBER() OVER (
        PARTITION BY ""UserId"", ""Platform"", ""CanonicalUrl""
        ORDER BY CASE WHEN ""Status"" = 1 AND ""IsDeleted"" = FALSE THEN 0 ELSE 1 END,
                 ""ClickCount"" DESC,
                 ""CreationTime"",
                 ""Id""
    ) AS ""Rank""
    FROM canonicalized
)
SELECT * FROM ranked;

UPDATE affiliate.""Conversion"" AS conversion
SET ""TrackingId"" = kept.""Id""
FROM ""CanonicalTracking"" AS duplicate
JOIN ""CanonicalTracking"" AS kept
    ON kept.""UserId"" = duplicate.""UserId""
    AND kept.""Platform"" = duplicate.""Platform""
    AND kept.""CanonicalUrl"" = duplicate.""CanonicalUrl""
    AND kept.""Rank"" = 1
WHERE duplicate.""Rank"" > 1
  AND conversion.""TrackingId"" = duplicate.""Id"";

UPDATE affiliate.""Click"" AS tracking_click
SET ""TrackingId"" = kept.""Id""
FROM ""CanonicalTracking"" AS duplicate
JOIN ""CanonicalTracking"" AS kept
    ON kept.""UserId"" = duplicate.""UserId""
    AND kept.""Platform"" = duplicate.""Platform""
    AND kept.""CanonicalUrl"" = duplicate.""CanonicalUrl""
    AND kept.""Rank"" = 1
WHERE duplicate.""Rank"" > 1
  AND tracking_click.""TrackingId"" = duplicate.""Id"";

DELETE FROM affiliate.""Tracking"" AS tracking
USING ""CanonicalTracking"" AS duplicate
WHERE duplicate.""Rank"" > 1
  AND tracking.""Id"" = duplicate.""Id"";

UPDATE affiliate.""Tracking"" AS tracking
SET ""NormalizedUrl"" = canonical.""CanonicalUrl""
FROM ""CanonicalTracking"" AS canonical
WHERE canonical.""Rank"" = 1
  AND tracking.""Id"" = canonical.""Id""
  AND tracking.""NormalizedUrl"" IS DISTINCT FROM canonical.""CanonicalUrl"";
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}

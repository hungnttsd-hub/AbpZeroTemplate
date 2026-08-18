using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;
using WebHoanTien.Affiliates;

namespace WebHoanTien.EntityFrameworkCore;

public static class AffiliateModelBuilderExtensions
{
    public static void ConfigureAffiliate(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));
        const string schema = WebHoanTienConsts.AffiliateDbSchema;

        builder.Entity<AffiliateTracking>(b =>
        {
            b.ToTable("Tracking", schema);
            b.ConfigureByConvention();
            b.Property(x => x.TrackingToken).HasMaxLength(64).IsRequired();
            b.Property(x => x.OriginalUrl).HasMaxLength(WebHoanTienConsts.UrlMaxLength).IsRequired();
            b.Property(x => x.NormalizedUrl).HasMaxLength(WebHoanTienConsts.UrlMaxLength).IsRequired();
            b.Property(x => x.AffiliateUrl).HasMaxLength(WebHoanTienConsts.UrlMaxLength);
            b.Property(x => x.ProductId).HasMaxLength(128);
            b.Property(x => x.ShopId).HasMaxLength(128);
            b.Property(x => x.ProductName).HasMaxLength(500);
            b.Property(x => x.ImageUrl).HasMaxLength(WebHoanTienConsts.UrlMaxLength);
            Money(b.Property(x => x.EstimatedCommission));
            b.HasIndex(x => x.TrackingToken).IsUnique();
            b.HasIndex(x => new { x.UserId, x.Platform, x.NormalizedUrl }).IsUnique()
                .HasFilter("\"Status\" = 1 AND \"IsDeleted\" = FALSE");
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AffiliateClick>(b =>
        {
            b.ToTable("Click", schema);
            b.ConfigureByConvention();
            b.Property(x => x.IpAddress).HasMaxLength(64);
            b.Property(x => x.UserAgent).HasMaxLength(1000);
            b.Property(x => x.Referer).HasMaxLength(WebHoanTienConsts.UrlMaxLength);
            b.HasIndex(x => new { x.TrackingId, x.ClickedAt });
            b.HasOne<AffiliateTracking>().WithMany().HasForeignKey(x => x.TrackingId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AffiliateConversion>(b =>
        {
            b.ToTable("Conversion", schema);
            b.ConfigureByConvention();
            b.Property(x => x.ExternalConversionId).HasMaxLength(256).IsRequired();
            b.Property(x => x.AttributionValue).HasMaxLength(256);
            Money(b.Property(x => x.GrossCommission));
            Money(b.Property(x => x.NetCommission));
            Rate(b.Property(x => x.UserShareRate));
            Money(b.Property(x => x.UserCommissionSnapshot));
            Money(b.Property(x => x.PlatformRevenueSnapshot));
            Money(b.Property(x => x.PayableUserCommission));
            b.HasIndex(x => new { x.Platform, x.ExternalConversionId }).IsUnique();
            b.HasIndex(x => new { x.UserId, x.PurchaseTime });
            b.HasOne<AffiliateTracking>().WithMany().HasForeignKey(x => x.TrackingId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AffiliateOrder>(b =>
        {
            b.ToTable("Order", schema);
            b.ConfigureByConvention();
            b.Property(x => x.ExternalOrderId).HasMaxLength(256).IsRequired();
            b.Property(x => x.ShopType).HasMaxLength(64);
            Money(b.Property(x => x.PurchaseAmount));
            Money(b.Property(x => x.NetCommission));
            Money(b.Property(x => x.UserCommissionSnapshot));
            Money(b.Property(x => x.PayableUserCommission));
            b.HasIndex(x => new { x.ConversionId, x.ExternalOrderId }).IsUnique();
            b.HasOne<AffiliateConversion>().WithMany().HasForeignKey(x => x.ConversionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AffiliateOrderItem>(b =>
        {
            b.ToTable("OrderItem", schema);
            b.ConfigureByConvention();
            b.Property(x => x.ExternalItemId).HasMaxLength(256).IsRequired();
            b.Property(x => x.ModelId).HasMaxLength(256).IsRequired();
            b.Property(x => x.ProductName).HasMaxLength(500);
            b.Property(x => x.ProviderStatus).HasMaxLength(128);
            Money(b.Property(x => x.PurchaseAmount));
            Money(b.Property(x => x.ItemTotalCommission));
            Money(b.Property(x => x.AllocatedNetCommission));
            Money(b.Property(x => x.UserCommissionSnapshot));
            Money(b.Property(x => x.RefundAmount));
            b.HasIndex(x => new { x.OrderId, x.ExternalItemId, x.ModelId }).IsUnique();
            b.HasOne<AffiliateOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AffiliateCommissionRule>(b =>
        {
            b.ToTable("CommissionRule", schema);
            b.ConfigureByConvention();
            Rate(b.Property(x => x.UserShareRate));
            b.HasIndex(x => new { x.Platform, x.EffectiveFrom });
        });

        builder.Entity<AffiliateSyncState>(b =>
        {
            b.ToTable("SyncState", schema);
            b.ConfigureByConvention();
            b.Property(x => x.LastError).HasMaxLength(2000);
            b.HasIndex(x => new { x.Platform, x.SyncKind }).IsUnique();
        });

        builder.Entity<AffiliateSyncRun>(b =>
        {
            b.ToTable("SyncRun", schema);
            b.ConfigureByConvention();
            b.Property(x => x.ErrorSummary).HasMaxLength(4000);
            b.HasIndex(x => new { x.Platform, x.SyncKind, x.StartedAt });
        });

        builder.Entity<AffiliateRawPayload>(b =>
        {
            b.ToTable("RawPayload", schema);
            b.ConfigureByConvention();
            b.Property(x => x.PayloadType).HasMaxLength(64).IsRequired();
            b.Property(x => x.SanitizedJson).HasColumnType("jsonb").IsRequired();
            b.HasIndex(x => x.ExpiresAt);
            b.HasOne<AffiliateSyncRun>().WithMany().HasForeignKey(x => x.SyncRunId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AffiliateConversion>().WithMany().HasForeignKey(x => x.ConversionId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<UserLegalConsent>(b =>
        {
            b.ToTable("UserLegalConsent", schema);
            b.ConfigureByConvention();
            b.Property(x => x.TermsVersion).HasMaxLength(64).IsRequired();
            b.Property(x => x.PrivacyVersion).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.UserId, x.TermsVersion, x.PrivacyVersion }).IsUnique();
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void Money(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal> property) => property.HasPrecision(20, 4);
    private static void Money(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal?> property) => property.HasPrecision(20, 4);
    private static void Rate(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal> property) => property.HasPrecision(7, 4);
}

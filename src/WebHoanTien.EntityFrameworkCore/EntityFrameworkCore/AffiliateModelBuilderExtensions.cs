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
            b.Property(x => x.AffiliateIdSnapshot).HasMaxLength(WebHoanTienConsts.AffiliateIdMaxLength);
            b.HasIndex(x => new { x.TrackingId, x.ClickedAt });
            b.HasIndex(x => x.UserAffiliateIdOverrideId);
            b.HasOne<AffiliateTracking>().WithMany().HasForeignKey(x => x.TrackingId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne<UserAffiliateIdOverride>().WithMany().HasForeignKey(x => x.UserAffiliateIdOverrideId)
                .OnDelete(DeleteBehavior.SetNull);
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
            Money(b.Property(x => x.SettledNetCommission));
            Money(b.Property(x => x.SettledUserCommission));
            b.Property(x => x.SettlementReference).HasMaxLength(128);
            b.HasIndex(x => new { x.ConversionId, x.ExternalOrderId }).IsUnique();
            b.HasIndex(x => new { x.Status, x.SettledAt });
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

        builder.Entity<UserAffiliateIdOverride>(b =>
        {
            b.ToTable("UserAffiliateIdOverride", schema);
            b.ConfigureByConvention();
            b.Property(x => x.AffiliateId).HasMaxLength(WebHoanTienConsts.AffiliateIdMaxLength).IsRequired();
            b.Property(x => x.AdminNote).HasMaxLength(WebHoanTienConsts.AffiliateOverrideNoteMaxLength);
            b.HasIndex(x => new { x.UserId, x.Platform }).IsUnique()
                .HasFilter("\"IsDeleted\" = FALSE");
            b.HasIndex(x => x.AffiliateId);
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
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

        builder.Entity<UserPayoutAccount>(b =>
        {
            b.ToTable("UserPayoutAccount", schema);
            b.ConfigureByConvention();
            b.Property(x => x.BankCode).HasMaxLength(32).IsRequired();
            b.Property(x => x.AccountNumber).HasMaxLength(30).IsRequired();
            b.Property(x => x.AccountHolderName).HasMaxLength(150).IsRequired();
            b.HasIndex(x => x.UserId).IsUnique();
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WithdrawalRequest>(b =>
        {
            b.ToTable("WithdrawalRequest", schema);
            b.ConfigureByConvention();
            b.Property(x => x.RequestCode).HasMaxLength(32).IsRequired();
            b.Property(x => x.BankCode).HasMaxLength(32).IsRequired();
            b.Property(x => x.AccountNumber).HasMaxLength(30).IsRequired();
            b.Property(x => x.AccountHolderName).HasMaxLength(150).IsRequired();
            b.Property(x => x.PaymentReference).HasMaxLength(128);
            b.Property(x => x.AdminNote).HasMaxLength(1000);
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            Money(b.Property(x => x.Amount));
            Money(b.Property(x => x.FeeAmount));
            Money(b.Property(x => x.NetAmount));
            b.HasIndex(x => x.RequestCode).IsUnique();
            b.HasIndex(x => new { x.UserId, x.CreationTime });
            b.HasIndex(x => new { x.Status, x.CreationTime });
            b.HasIndex(x => x.UserId).IsUnique()
                .HasFilter("\"Status\" = 1 AND \"IsDeleted\" = FALSE");
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<UserPayoutAccount>().WithMany().HasForeignKey(x => x.PayoutAccountId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.ProcessedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<WithdrawalPaymentProof>(b =>
        {
            b.ToTable("WithdrawalPaymentProof", schema);
            b.ConfigureByConvention();
            b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            b.Property(x => x.Content).HasColumnType("bytea").IsRequired();
            b.HasIndex(x => x.WithdrawalRequestId).IsUnique();
            b.HasOne<WithdrawalRequest>().WithMany().HasForeignKey(x => x.WithdrawalRequestId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void Money(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal> property) => property.HasPrecision(20, 4);
    private static void Money(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal?> property) => property.HasPrecision(20, 4);
    private static void Rate(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal> property) => property.HasPrecision(7, 4);
}

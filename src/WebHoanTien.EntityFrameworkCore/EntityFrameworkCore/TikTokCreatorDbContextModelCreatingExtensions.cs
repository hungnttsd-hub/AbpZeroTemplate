using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.EntityFrameworkCore;

public static class TikTokCreatorDbContextModelCreatingExtensions
{
    public static void ConfigureTikTokCreator(this ModelBuilder builder)
    {
        builder.Entity<TikTokDeveloperAccount>(b =>
        {
            b.ToTable("TikTokDeveloperAccounts", "affiliate"); b.ConfigureByConvention();
            b.Property(x => x.AppKey).HasMaxLength(128).IsRequired();
            b.Property(x => x.OpenId).HasMaxLength(256); b.Property(x => x.Username).HasMaxLength(256);
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.Property(x => x.AccessTokenEncrypted).IsRequired(); b.Property(x => x.RefreshTokenEncrypted).IsRequired();
            b.Property(x => x.GrantedScopesJson).IsRequired(); b.Property(x => x.TokenVersion).IsConcurrencyToken();
        });
        builder.Entity<TikTokDeveloperAuthState>(b =>
        {
            b.ToTable("TikTokDeveloperAuthStates", "affiliate"); b.ConfigureByConvention();
            b.Property(x => x.AppKey).HasMaxLength(128).IsRequired(); b.Property(x => x.StateHash).HasMaxLength(64).IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.HasIndex(x => x.StateHash).IsUnique(); b.HasIndex(x => new { x.OwnerUserId, x.AppKey });
            b.HasIndex(x => x.ExpiresAtUtc);
        });
        builder.Entity<TikTokDeveloperApiKey>(b =>
        {
            b.ToTable("TikTokDeveloperApiKeys", "affiliate"); b.ConfigureByConvention();
            b.Property(x => x.AppKey).HasMaxLength(128).IsRequired(); b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.KeyHash).HasMaxLength(64).IsRequired(); b.Property(x => x.KeyPreview).HasMaxLength(32).IsRequired();
            b.HasIndex(x => x.KeyHash).IsUnique(); b.HasIndex(x => new { x.OwnerUserId, x.AppKey });
        });
        builder.Entity<TikTokCreatorCredentials>(b =>
        {
            b.ToTable("TikTokCreatorCredentials", "affiliate");
            b.ConfigureByConvention();
            b.Property(x => x.AppKey).HasMaxLength(128).IsRequired();
            b.Property(x => x.OpenId).HasMaxLength(256).IsRequired();
            b.Property(x => x.CreatorUserId).HasMaxLength(256);
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.Property(x => x.AccessTokenEncrypted).IsRequired();
            b.Property(x => x.RefreshTokenEncrypted).IsRequired();
            b.Property(x => x.GrantedScopesJson).IsRequired();
            b.Property(x => x.TokenVersion).IsConcurrencyToken();
            b.HasIndex(x => new { x.AppKey, x.OpenId }).IsUnique();
            b.HasIndex(x => x.AppKey).IsUnique().HasFilter("\"IsPrimary\" = TRUE");
        });
        builder.Entity<TikTokCreatorAuthState>(b =>
        {
            b.ToTable("TikTokCreatorAuthStates", "affiliate");
            b.ConfigureByConvention();
            b.Property(x => x.StateHash).HasMaxLength(64).IsRequired();
            b.Property(x => x.AppKey).HasMaxLength(128).IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.HasIndex(x => x.StateHash).IsUnique();
            b.HasIndex(x => x.ExpiresAtUtc);
        });
    }
}

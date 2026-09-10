using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.Identity;
using WebHoanTien.IdentityExtensions;

namespace WebHoanTien.EntityFrameworkCore;

public static class AnonymousAccountModelBuilder
{
    public static void ConfigureAnonymousAccounts(this ModelBuilder builder)
    {
        builder.Entity<IdentityUser>(b =>
        {
            b.Property<int>(CatBackAccountProperties.Type).HasDefaultValue(1).ValueGeneratedNever();
            b.Property<string>(CatBackAccountProperties.LoginEmail).HasMaxLength(256);
            b.Property<string>(CatBackAccountProperties.NormalizedLoginEmail).HasMaxLength(256);
            b.HasIndex(CatBackAccountProperties.NormalizedLoginEmail).IsUnique()
                .HasFilter("\"NormalizedLoginEmail\" IS NOT NULL");
        });
        builder.Entity<AnonymousRecovery>(b =>
        {
            b.ToTable("CatBackAnonymousRecovery"); b.ConfigureByConvention();
            b.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
            b.Property(x => x.ProtectedCode).HasMaxLength(2048).IsRequired();
            b.HasIndex(x => x.CodeHash).IsUnique();
            b.HasIndex(x => x.UserId).IsUnique().HasFilter("\"RevokedAt\" IS NULL");
            b.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<AnonymousDevice>(b =>
        {
            b.ToTable("CatBackAnonymousDevice"); b.ConfigureByConvention();
            b.Property(x => x.SecretHash).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.SecretHash).IsUnique();
            b.HasIndex(x => x.UserId);
            b.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<PendingAccountUpgrade>(b =>
        {
            b.ToTable("CatBackPendingAccountUpgrade"); b.ConfigureByConvention();
            b.Property(x => x.UserName).HasMaxLength(256);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.PasswordHash).HasMaxLength(1024);
            b.Property(x => x.TokenHash).HasMaxLength(64);
            b.Property(x => x.ReturnUrl).HasMaxLength(2048);
            b.Property(x => x.TermsVersion).HasMaxLength(64);
            b.Property(x => x.PrivacyVersion).HasMaxLength(64);
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasIndex(x => x.UserId).IsUnique().HasFilter("\"RevokedAt\" IS NULL");
            b.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

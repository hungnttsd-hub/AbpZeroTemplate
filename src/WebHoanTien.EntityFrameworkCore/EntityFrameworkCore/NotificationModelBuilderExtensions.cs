using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;
using WebHoanTien.Notifications;

namespace WebHoanTien.EntityFrameworkCore;

public static class NotificationModelBuilderExtensions
{
    public static void ConfigureNotifications(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));
        const string schema = WebHoanTienConsts.NotificationDbSchema;

        builder.Entity<CustomerNotification>(b =>
        {
            b.ToTable("CustomerNotification", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).HasMaxLength(WebHoanTienConsts.NotificationTitleMaxLength).IsRequired();
            b.Property(x => x.Message).HasMaxLength(WebHoanTienConsts.NotificationMessageMaxLength).IsRequired();
            b.Property(x => x.ActionUrl).HasMaxLength(WebHoanTienConsts.NotificationActionUrlMaxLength);
            b.Property(x => x.EventKey).HasMaxLength(WebHoanTienConsts.NotificationEventKeyMaxLength).IsRequired();
            b.HasIndex(x => new { x.UserId, x.EventKey }).IsUnique();
            b.HasIndex(x => new { x.UserId, x.IsRead, x.CreationTime });
            b.HasIndex(x => new { x.UserId, x.Category, x.CreationTime });
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NotificationCampaign>(b =>
        {
            b.ToTable("NotificationCampaign", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).HasMaxLength(WebHoanTienConsts.NotificationTitleMaxLength).IsRequired();
            b.Property(x => x.Message).HasMaxLength(WebHoanTienConsts.NotificationMessageMaxLength).IsRequired();
            b.Property(x => x.ActionUrl).HasMaxLength(WebHoanTienConsts.NotificationActionUrlMaxLength);
            b.HasIndex(x => x.PublishedAt);
            b.HasIndex(x => x.TargetUserId);
            b.HasOne<Volo.Abp.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.TargetUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

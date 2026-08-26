using System;
using WebHoanTien.Notifications;

namespace WebHoanTien.Web.Pages.Notifications;

public static class NotificationPageUi
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "CatsBack-Vietnam", TimeSpan.FromHours(7), "Việt Nam", "Việt Nam");

    public static bool IsToday(DateTime value)
    {
        var local = ToVietnamTime(value);
        var now = ToVietnamTime(DateTime.UtcNow);
        return local.Date == now.Date;
    }

    public static string FormatTime(DateTime value)
    {
        var local = ToVietnamTime(value);
        var now = ToVietnamTime(DateTime.UtcNow);
        var elapsed = now - local;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
        if (elapsed.TotalMinutes < 1) return "Vừa xong";
        if (elapsed.TotalMinutes < 60) return $"{Math.Max(1, (int)elapsed.TotalMinutes)} phút trước";
        if (elapsed.TotalHours < 24 && local.Date == now.Date)
            return $"{Math.Max(1, (int)elapsed.TotalHours)} giờ trước";
        if (local.Date == now.Date) return $"Hôm nay, {local:HH:mm}";
        if (local.Date == now.Date.AddDays(-1)) return "Hôm qua";
        return local.ToString("dd/MM/yyyy");
    }

    public static string Icon(CustomerNotificationKind kind) => kind switch
    {
        CustomerNotificationKind.CashbackRecorded => "cashback.svg",
        CustomerNotificationKind.CashbackPending => "pending.svg",
        CustomerNotificationKind.OrderReconciled => "order.svg",
        CustomerNotificationKind.OrderCancelled or CustomerNotificationKind.OrderRefunded or
            CustomerNotificationKind.OrderRejected => "status-negative.svg",
        CustomerNotificationKind.WithdrawalRejected or CustomerNotificationKind.WithdrawalCancelled => "status-negative.svg",
        CustomerNotificationKind.PayoutAccountUpdated => "bank.svg",
        CustomerNotificationKind.Promotion => "promotion.svg",
        _ => "wallet.svg"
    };

    public static string Tone(CustomerNotificationKind kind) => kind switch
    {
        CustomerNotificationKind.CashbackRecorded => "cashback",
        CustomerNotificationKind.CashbackPending => "pending",
        CustomerNotificationKind.OrderReconciled => "order",
        CustomerNotificationKind.OrderCancelled or CustomerNotificationKind.OrderRefunded or
            CustomerNotificationKind.OrderRejected or CustomerNotificationKind.WithdrawalRejected or
            CustomerNotificationKind.WithdrawalCancelled => "negative",
        CustomerNotificationKind.Promotion => "promotion",
        CustomerNotificationKind.PayoutAccountUpdated => "bank",
        CustomerNotificationKind.WithdrawalPaid => "cashback",
        _ => "wallet"
    };

    private static DateTime ToVietnamTime(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
    }
}

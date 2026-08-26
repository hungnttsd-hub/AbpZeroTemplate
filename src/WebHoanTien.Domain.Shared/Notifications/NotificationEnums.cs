namespace WebHoanTien.Notifications;

public enum CustomerNotificationCategory
{
    Cashback = 1,
    Order = 2,
    Wallet = 3,
    Promotion = 4
}

public enum CustomerNotificationKind
{
    CashbackPending = 1,
    CashbackRecorded = 2,
    OrderReconciled = 10,
    OrderCancelled = 11,
    OrderRefunded = 12,
    OrderRejected = 13,
    WithdrawalPending = 20,
    WithdrawalPaid = 21,
    WithdrawalRejected = 22,
    WithdrawalCancelled = 23,
    PayoutAccountUpdated = 24,
    Promotion = 30
}

public enum NotificationAudience
{
    AllUsers = 1,
    SingleUser = 2
}

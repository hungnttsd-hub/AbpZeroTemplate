using System;

namespace WebHoanTien.Affiliates;

public static class WithdrawalTransferContent
{
    public static string Create(string requestCode)
    {
        if (string.IsNullOrWhiteSpace(requestCode))
        {
            throw new ArgumentException("Withdrawal request code is required.", nameof(requestCode));
        }

        return $"CATBACK {requestCode.Trim().ToUpperInvariant()}";
    }
}

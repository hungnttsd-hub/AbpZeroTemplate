using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class UserPayoutAccount : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string BankCode { get; private set; } = null!;
    public string AccountNumber { get; private set; } = null!;
    public string AccountHolderName { get; private set; } = null!;

    protected UserPayoutAccount() { }

    public UserPayoutAccount(Guid id, Guid userId, string bankCode, string accountNumber, string accountHolderName)
        : base(id)
    {
        UserId = userId;
        Update(bankCode, accountNumber, accountHolderName);
    }

    public void Update(string bankCode, string accountNumber, string accountHolderName)
    {
        BankCode = bankCode.Trim().ToUpperInvariant();
        AccountNumber = accountNumber.Trim();
        AccountHolderName = accountHolderName.Trim().ToUpperInvariant();
    }
}

using System;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class WithdrawalPaymentProof : CreationAuditedAggregateRoot<Guid>
{
    public Guid WithdrawalRequestId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long Size { get; private set; }
    public string Sha256 { get; private set; } = null!;
    public byte[] Content { get; private set; } = null!;

    protected WithdrawalPaymentProof() { }

    public WithdrawalPaymentProof(Guid id, Guid withdrawalRequestId, string fileName, string contentType,
        string sha256, byte[] content) : base(id)
    {
        WithdrawalRequestId = withdrawalRequestId;
        FileName = Check.NotNullOrWhiteSpace(fileName, nameof(fileName), 255).Trim();
        ContentType = Check.NotNullOrWhiteSpace(contentType, nameof(contentType), 100).Trim().ToLowerInvariant();
        Sha256 = Check.NotNullOrWhiteSpace(sha256, nameof(sha256), 64).Trim().ToLowerInvariant();
        Content = Check.NotNull(content, nameof(content)).ToArray();
        Size = Content.LongLength;
    }
}

using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateRawPayload : CreationAuditedEntity<Guid>
{
    public Guid SyncRunId { get; private set; }
    public Guid? ConversionId { get; private set; }
    public string PayloadType { get; private set; } = null!;
    public string SanitizedJson { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }

    protected AffiliateRawPayload() { }
    public AffiliateRawPayload(Guid id, Guid syncRunId, Guid? conversionId, string type, string sanitizedJson, DateTime expiresAt) : base(id)
    { SyncRunId = syncRunId; ConversionId = conversionId; PayloadType = type; SanitizedJson = sanitizedJson; ExpiresAt = expiresAt; }
}

using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class UserAffiliateIdOverride : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public AffiliatePlatform Platform { get; private set; }
    public string AffiliateId { get; private set; } = null!;
    public string? AdminNote { get; private set; }

    protected UserAffiliateIdOverride() { }

    public UserAffiliateIdOverride(Guid id, Guid userId, AffiliatePlatform platform, string affiliateId,
        string? adminNote = null) : base(id)
    {
        UserId = userId;
        Platform = platform;
        Change(affiliateId, adminNote);
    }

    public void Change(string affiliateId, string? adminNote)
    {
        AffiliateId = AffiliateIdRules.Normalize(affiliateId);
        AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        if (AdminNote?.Length > WebHoanTienConsts.AffiliateOverrideNoteMaxLength)
            throw new ArgumentException($"Ghi chú không được vượt quá {WebHoanTienConsts.AffiliateOverrideNoteMaxLength} ký tự.", nameof(adminNote));
    }
}

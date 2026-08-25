using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class UserLegalConsent : CreationAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string TermsVersion { get; private set; } = null!;
    public string PrivacyVersion { get; private set; } = null!;
    public LegalConsentMethod Method { get; private set; }
    public DateTime ConsentedAt { get; private set; }

    protected UserLegalConsent() { }
    public UserLegalConsent(Guid id, Guid userId, string termsVersion, string privacyVersion, LegalConsentMethod method, DateTime consentedAt) : base(id)
    { UserId = userId; TermsVersion = termsVersion; PrivacyVersion = privacyVersion; Method = method; ConsentedAt = consentedAt; }
}

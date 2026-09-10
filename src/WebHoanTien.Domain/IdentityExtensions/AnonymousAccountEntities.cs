using System;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace WebHoanTien.IdentityExtensions;

[DisableAuditing]
public class AnonymousRecovery : AggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = "";
    public string ProtectedCode { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    protected AnonymousRecovery() { }
    public AnonymousRecovery(Guid id) : base(id) { }
}

[DisableAuditing]
public class AnonymousDevice : AggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    public string SecretHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    protected AnonymousDevice() { }
    public AnonymousDevice(Guid id) : base(id) { }
}

[DisableAuditing]
public class PendingAccountUpgrade : AggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string ReturnUrl { get; set; } = "/";
    public string TermsVersion { get; set; } = "";
    public string PrivacyVersion { get; set; } = "";
    public DateTime AcceptedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    protected PendingAccountUpgrade() { }
    public PendingAccountUpgrade(Guid id) : base(id) { }
}

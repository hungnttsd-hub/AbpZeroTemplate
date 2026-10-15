using System;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace WebHoanTien.TikTokAffiliate;

// Id is the host Identity user's ID. These credentials never fall back to CatBack's primary Creator.
[DisableAuditing]
public class TikTokDeveloperAccount : Entity<Guid>
{
    public string AppKey { get; set; } = "";
    public string? OpenId { get; set; }
    public string? Username { get; set; }
    public string AccessTokenEncrypted { get; set; } = "";
    public string RefreshTokenEncrypted { get; set; } = "";
    public string GrantedScopesJson { get; set; } = "[]";
    public string Status { get; set; } = "NotConnected";
    public DateTimeOffset? AccessExpiresAtUtc { get; set; }
    public DateTimeOffset? RefreshExpiresAtUtc { get; set; }
    public long TokenVersion { get; set; }
    public long AuthorizationEpoch { get; set; }
    public Guid? RefreshLeaseId { get; set; }
    public DateTimeOffset? RefreshLeaseUntilUtc { get; set; }
    public DateTimeOffset? RateWindowUtc { get; set; }
    public int RateCount { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    protected TikTokDeveloperAccount() { }
    public TikTokDeveloperAccount(Guid ownerId) : base(ownerId) { }
}

[DisableAuditing]
public class TikTokDeveloperAuthState : Entity<Guid>
{
    public Guid OwnerUserId { get; set; }
    public string AppKey { get; set; } = "";
    public string StateHash { get; set; } = "";
    public long AuthorizationEpoch { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset ExpiresAtUtc { get; set; }
    protected TikTokDeveloperAuthState() { }
    public TikTokDeveloperAuthState(Guid id) : base(id) { }
}

[DisableAuditing]
public class TikTokDeveloperApiKey : Entity<Guid>
{
    public Guid OwnerUserId { get; set; }
    public string AppKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string KeyHash { get; set; } = "";
    public string KeyPreview { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    protected TikTokDeveloperApiKey() { }
    public TikTokDeveloperApiKey(Guid id) : base(id) { }
}

using System;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace WebHoanTien.TikTokAffiliate;

[DisableAuditing]
public class TikTokCreatorCredentials : Entity<Guid>
{
    public string AppKey { get; set; } = "";
    public string OpenId { get; set; } = "";
    public string? CreatorUserId { get; set; }
    public string AccessTokenEncrypted { get; set; } = "";
    public string RefreshTokenEncrypted { get; set; } = "";
    public DateTimeOffset AccessTokenExpiresAtUtc { get; set; }
    public DateTimeOffset RefreshTokenExpiresAtUtc { get; set; }
    public string GrantedScopesJson { get; set; } = "[]";
    public int UserType { get; set; } = 1;
    public string Status { get; set; } = "Active";
    public bool IsPrimary { get; set; }
    public Guid AuthorizedByAdminId { get; set; }
    public long TokenVersion { get; set; }
    public Guid? RefreshLockId { get; set; }
    public DateTimeOffset? RefreshLockUntilUtc { get; set; }
    public DateTimeOffset? LastRefreshedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    protected TikTokCreatorCredentials() { }
    public TikTokCreatorCredentials(Guid id) : base(id) { }
}

[DisableAuditing]
public class TikTokCreatorAuthState : Entity<Guid>
{
    public string StateHash { get; set; } = "";
    public string AppKey { get; set; } = "";
    public Guid InitiatedByAdminId { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAtUtc { get; set; }

    protected TikTokCreatorAuthState() { }
    public TikTokCreatorAuthState(Guid id) : base(id) { }
}

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using WebHoanTien.Affiliates;
using WebHoanTien.Notifications;

namespace WebHoanTien.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ISettingManagementDbContext))]
[ConnectionStringName("Default")]
public class WebHoanTienDbContext :
    AbpDbContext<WebHoanTienDbContext>,
    IIdentityDbContext,
    ISettingManagementDbContext,
    IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<IdentityUser> Users { get; set; } = null!;
    public DbSet<IdentityRole> Roles { get; set; } = null!;
    public DbSet<IdentityClaimType> ClaimTypes { get; set; } = null!;
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; } = null!;
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; } = null!;
    public DbSet<IdentityLinkUser> LinkUsers { get; set; } = null!;
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; } = null!;
    public DbSet<IdentitySession> Sessions { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<SettingDefinitionRecord> SettingDefinitionRecords { get; set; } = null!;
    public DbSet<AffiliateTracking> AffiliateTrackings { get; set; } = null!;
    public DbSet<AffiliateClick> AffiliateClicks { get; set; } = null!;
    public DbSet<AffiliateConversion> AffiliateConversions { get; set; } = null!;
    public DbSet<AffiliateOrder> AffiliateOrders { get; set; } = null!;
    public DbSet<AffiliateOrderItem> AffiliateOrderItems { get; set; } = null!;
    public DbSet<AffiliateOrderItemAttribution> AffiliateOrderItemAttributions { get; set; } = null!;
    public DbSet<AffiliateCommissionRule> AffiliateCommissionRules { get; set; } = null!;
    public DbSet<AffiliateSyncState> AffiliateSyncStates { get; set; } = null!;
    public DbSet<AffiliateSyncRun> AffiliateSyncRuns { get; set; } = null!;
    public DbSet<AffiliateRawPayload> AffiliateRawPayloads { get; set; } = null!;
    public DbSet<ShopeeSettlementBatch> ShopeeSettlementBatches { get; set; } = null!;
    public DbSet<ShopeeSettlementBill> ShopeeSettlementBills { get; set; } = null!;
    public DbSet<ShopeeSettlementRecord> ShopeeSettlementRecords { get; set; } = null!;
    public DbSet<UserAffiliateIdOverride> UserAffiliateIdOverrides { get; set; } = null!;
    public DbSet<UserLegalConsent> UserLegalConsents { get; set; } = null!;
    public DbSet<UserPayoutAccount> UserPayoutAccounts { get; set; } = null!;
    public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; } = null!;
    public DbSet<WithdrawalPaymentProof> WithdrawalPaymentProofs { get; set; } = null!;
    public DbSet<CustomerNotification> CustomerNotifications { get; set; } = null!;
    public DbSet<NotificationCampaign> NotificationCampaigns { get; set; } = null!;

    public WebHoanTienDbContext(DbContextOptions<WebHoanTienDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureAffiliate();
        builder.ConfigureNotifications();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations.RioHub;

namespace WebHoanTien.TikTokAffiliate;

[Authorize(TikTokAffiliateAccess.Policy)]
[RemoteService(false)]
public class TikTokAffiliateRioHubService : WebHoanTienAppService, ITikTokAffiliateService
{
    private readonly IRioHubAffiliateClient _client;
    private readonly RioHubOptions _options;
    private readonly TikTokAffiliateLinkCreator _creator;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<RioHubOrderSku, Guid> _orders;
    private readonly IIdentityUserRepository _users;
    private readonly ITikTokCreatorConnection _connection;

    public TikTokAffiliateRioHubService(IRioHubAffiliateClient client, IOptions<RioHubOptions> options,
        TikTokAffiliateLinkCreator creator, IRepository<AffiliateTracking, Guid> trackings,
        IRepository<RioHubOrderSku, Guid> orders, IIdentityUserRepository users, ITikTokCreatorConnection connection)
    { _client = client; _options = options.Value; _creator = creator; _trackings = trackings; _orders = orders; _users = users; _connection = connection; }

    private bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        Regex.IsMatch(_options.CreatorUsername, @"\A[A-Za-z0-9._]{1,100}\z");

    public Task<TikTokAffiliateIntegrationDto> GetIntegrationInfo() => Task.FromResult(
        new TikTokAffiliateIntegrationDto("RioHub", false, null, IsAvailable: IsConfigured));

    public async Task<TikTokCreatorDto?> GetCreatorProfile()
    {
        var user = await EligibleUserAsync();
        return HasLocalConnection(user) ? LocalCreator(user) : await _connection.GetProfile();
    }

    // This is a per-account CatBack connection, not TikTok OAuth or RioHub authorization.
    public async Task<TikTokCreatorDto> ConnectCreator()
    {
        var user = await EligibleUserAsync();
        if (!TikTokCreatorSelection.IsLocal(user.GetProperty<string>(TikTokAffiliateAccess.PendingCreatorProperty)))
            throw new UserFriendlyException("Hãy nhập Username TikTok và bắt đầu kết nối từ CatBack.");
        user.SetProperty(TikTokAffiliateAccess.WorkspaceConnectionProperty, true);
        user.SetProperty(TikTokAffiliateAccess.WorkspaceCreatorProperty, TikTokAffiliateAccess.LocalCreatorUsername);
        await _users.UpdateAsync(user, autoSave: true);
        return LocalCreator(user);
    }

    public async Task DisconnectCreator()
    {
        var user = await EligibleUserAsync();
        await _connection.Disconnect();
        user.SetProperty(TikTokAffiliateAccess.WorkspaceConnectionProperty, false);
        user.SetProperty(TikTokAffiliateAccess.WorkspaceCreatorProperty, string.Empty);
        user.SetProperty(TikTokAffiliateAccess.PendingCreatorProperty, string.Empty);
        await _users.UpdateAsync(user, autoSave: true);
    }

    private async Task<IdentityUser> EligibleUserAsync()
    {
        if (!CurrentUser.IsAuthenticated) throw new UserFriendlyException("Vui lòng đăng nhập CatBack.");
        var user = await _users.FindAsync(CurrentUser.GetId());
        if (user is not { IsActive: true } || !user.GetProperty<bool>(TikTokAffiliateAccess.UserProperty))
            throw new UserFriendlyException("Tài khoản chưa được cấp quyền dùng TikTok Affiliate.");
        return user;
    }

    private static bool HasLocalConnection(IdentityUser user) =>
        user.GetProperty<bool>(TikTokAffiliateAccess.WorkspaceConnectionProperty) &&
        TikTokCreatorSelection.IsLocal(user.GetProperty<string>(TikTokAffiliateAccess.WorkspaceCreatorProperty));

    private static TikTokCreatorDto LocalCreator(IdentityUser user)
    {
        var username = user.GetProperty<string>(TikTokAffiliateAccess.WorkspaceCreatorProperty) ?? user.UserName;
        return new TikTokCreatorDto(string.Empty, username, "@" + username, string.Empty, "CONNECTED_LOCAL");
    }

    public async Task<TikTokProductDto> GetProduct(string productUrl)
    {
        var value = productUrl?.Trim() ?? string.Empty;
        var id = Regex.IsMatch(value, @"\A[0-9]{1,30}\z") ? value :
            TikTokAffiliateUrl.TryNormalize(value, out _, out var productId) ? productId : null;
        if (id is null) throw new UserFriendlyException("Tra cứu riêng cần ID sản phẩm hoặc link trực tiếp có ID. Với link rút gọn, dùng Tạo link TikTok để nhận link và thông tin sản phẩm.");
        var configured = RioHubProductMapper.FindOverride(id);
        if (configured is not null) return configured;
        RequireConfigured();
        try
        {
            var response = await _client.GetProductsAsync(new RioHubProductsQuery { CreatorUsername = _options.CreatorUsername, ProductId = id });
            if (response.TryGetProperty("products", out var products) && products.ValueKind == JsonValueKind.Array)
                foreach (var product in products.EnumerateArray())
                    if (product.ValueKind == JsonValueKind.Object && product.TryGetProperty("id", out var returnedId) &&
                        returnedId.ValueKind == JsonValueKind.String && returnedId.GetString() == id)
                        return RioHubProductMapper.Map(product, id);
            throw new UserFriendlyException("Không tìm thấy sản phẩm đủ điều kiện affiliate cho nhà sáng tạo RioHub.");
        }
        catch (RioHubApiException exception) { throw new UserFriendlyException(exception.Message); }
    }

    public async Task<TikTokGeneratedLinkDto> GenerateAffiliateLink(string productId)
    {
        if (await GetCreatorProfile() is null)
            throw new UserFriendlyException("Vui lòng kết nối tài khoản trước khi tạo link TikTok.");
        var input = productId?.Trim() ?? string.Empty;
        var url = Regex.IsMatch(input, @"\A[0-9]{1,30}\z") ? "https://www.tiktok.com/view/product/" + input : input;
        if (!TikTokAffiliateUrl.TryNormalize(url, out _, out var inputId) || TikTokProductOverrides.Find(inputId) is null)
            RequireConfigured();
        var result = await _creator.CreateAsync(url, CurrentUser.GetId());
        var product = result.Product.HasValue ? RioHubProductMapper.Map(result.Product.Value, result.Tracking.ProductId!)
            : RioHubProductMapper.FindOverride(result.Tracking.ProductId);
        return MapLink(result.Tracking) with { Product = product };
    }

    public async Task<string?> GetLink(string productId) => (await OwnedTrackingsAsync())
        .Where(x => !x.IsHidden && x.ProductId == productId).OrderByDescending(x => x.CreationTime)
        .Select(x => "/go/" + x.TrackingToken).FirstOrDefault();

    public async Task<IReadOnlyList<TikTokGeneratedLinkDto>> GetGeneratedLinks() =>
        (await OwnedTrackingsAsync()).Where(x => !x.IsHidden && !string.IsNullOrWhiteSpace(x.AffiliateUrl))
            .OrderByDescending(x => x.CreationTime).Select(MapLink).ToArray();

    public async Task DeleteGeneratedLink(string productId)
    {
        // The legacy interface parameter name is kept; the UI passes the tracking UUID, never a global product ID.
        if (!Guid.TryParse(productId, out var trackingId)) throw new UserFriendlyException("Mã liên kết không hợp lệ.");
        var tracking = (await OwnedTrackingsAsync()).FirstOrDefault(x => x.Id == trackingId);
        if (tracking is null) throw new UserFriendlyException("Không tìm thấy liên kết của bạn.");
        tracking.Hide(DateTime.UtcNow);
        await _trackings.UpdateAsync(tracking, autoSave: true);
    }

    public async Task<IReadOnlyList<TikTokAffiliateOrderDto>> SearchAffiliateOrders()
    {
        var manualOrders = await ManualOrdersForCurrentUserAsync();
        var tokens = (await OwnedTrackingsAsync())
            .Where(x => !x.GetProperty<bool>(TikTokProductOverrides.ProductUrlFallbackProperty))
            .Select(x => x.TrackingToken).ToArray();
        if (tokens.Length == 0) return manualOrders;
        var creator = _options.CreatorUsername.ToLowerInvariant();
        // Match an exact sub_id first; only use a SPECIFIC trace_id when sub_id is absent.
        // General links and conflicting attribution fields must not expose another customer's orders.
        var query = (await _orders.GetQueryableAsync()).Where(x => x.CreatorUsername == creator &&
            ((x.SubId != null && tokens.Contains(x.SubId) && (x.TraceType == null || x.TraceType == "SPECIFIC")) ||
             ((x.SubId == null || x.SubId == "") && x.TraceType == "SPECIFIC" && x.TraceId != null && tokens.Contains(x.TraceId))))
            .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.OrderId).ThenBy(x => x.SkuId);
        var rows = await AsyncExecuter.ToListAsync(query);
        return rows.Select(x => new TikTokAffiliateOrderDto(x.OrderId, x.ProductName ?? x.ProductId ?? "Sản phẩm TikTok Shop",
            null, x.Status == 2 ? x.ActualCommission : x.Status == 1 ? x.EstimatedCommission : null,
            x.Currency, x.Status switch { 1 => "Pending", 2 => "Settled", _ => "Cancelled" }, Utc(x.CreatedAtUtc),
            x.SkuId, x.EstimatedCommission, x.ActualCommission, x.SettledAtUtc.HasValue ? Utc(x.SettledAtUtc.Value) : null,
            x.ProductId)).Concat(manualOrders).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.OrderId).ToArray();
    }

    private async Task<IReadOnlyList<TikTokAffiliateOrderDto>> ManualOrdersForCurrentUserAsync()
    {
        // Check the persisted flag; neither client input nor a stale cookie can enable this row.
        var user = await _users.FindAsync(CurrentUser.GetId());
        if (user is not { IsActive: true } || !user.GetProperty<bool>(TikTokAffiliateAccess.UserProperty) || !HasLocalConnection(user))
            return Array.Empty<TikTokAffiliateOrderDto>();

        var product = TikTokProductOverrides.Find(TikTokProductOverrides.SunSetProductId)!;
        // One fixed manual order per eligible account; repeated refreshes never add another row.
        // It is display data only, kept outside the provider ledger and wallet settlement.
        return new[]
        {
            new TikTokAffiliateOrderDto("CB-TT-20261009-01", product.Title, product.Price,
                product.CommissionAmount, "VND", "Pending",
                new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.FromHours(7)),
                "CB-SUNSET-01", EstimatedCommission: product.CommissionAmount,
                ProductId: product.ProductId, Source: "Manual")
        };
    }

    private async Task<List<AffiliateTracking>> OwnedTrackingsAsync()
    {
        var userId = CurrentUser.GetId();
        return (await _trackings.GetListAsync(x => x.UserId == userId && x.Platform == AffiliatePlatform.TikTok &&
            x.Status == AffiliateTrackingStatus.Active)).Where(x => string.Equals(
                x.GetProperty<string>("RioHubCreatorUsername"), _options.CreatorUsername, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static TikTokGeneratedLinkDto MapLink(AffiliateTracking tracking) => new(
        tracking.ProductId ?? string.Empty, TikTokProductOverrides.Find(tracking.ProductId)?.Title ?? tracking.ProductName ?? "Sản phẩm TikTok Shop", "/go/" + tracking.TrackingToken,
        Utc(tracking.CreationTime), tracking.GetProperty<bool>(TikTokProductOverrides.ProductUrlFallbackProperty) ? "ProductUrl" : "Generated", tracking.Id, tracking.AffiliateUrl,
        RioHubProductMapper.FindOverride(tracking.ProductId));
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private void RequireConfigured()
    {
        if (!IsConfigured) throw new UserFriendlyException("Tạo link TikTok chưa sẵn sàng. Vui lòng liên hệ hỗ trợ.");
    }
}

using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Integrations;

// Short-lived live authorization trial. Use the configured cache and Data Protection key ring.
// This does not share token state with the mock service or expose it as an Identity extra property.
public class TikTokCreatorConnection : ITikTokCreatorConnection, ITransientDependency
{
    private readonly IDistributedCache _cache;
    private readonly IDataProtectionProvider _protection;
    private readonly ICurrentUser _user;
    private readonly ICurrentTenant _tenant;
    private readonly TikTokCreatorOAuthClient _client;
    public bool IsConfigured => _client.IsConfigured;
    private string Owner => $"{_tenant.Id}:{_user.GetId()}";
    private string Key => "CatBack:TikTokCreator:trial:" + Owner;
    private IDataProtector Protector => _protection.CreateProtector("CatBack.TikTokCreator.Tokens.v1", Owner, _client.AppKey);
    public TikTokCreatorConnection(IDistributedCache cache, IDataProtectionProvider protection,
        ICurrentUser user, ICurrentTenant tenant, TikTokCreatorOAuthClient client)
    { _cache = cache; _protection = protection; _user = user; _tenant = tenant; _client = client; }

    public async Task<TikTokCreatorDto> Complete(string code)
    {
        var tokens = await _client.Exchange(code);
        var profile = await _client.GetProfile(tokens);
        await Save(tokens);
        return profile;
    }
    public async Task<TikTokCreatorDto?> GetProfile()
    {
        if (!_user.IsAuthenticated || !IsConfigured) return null;
        var ciphertext = await _cache.GetStringAsync(Key);
        if (ciphertext is null) return null;
        TikTokCreatorTokens? tokens;
        try { tokens = JsonSerializer.Deserialize<TikTokCreatorTokens>(Protector.Unprotect(ciphertext)); }
        catch (CryptographicException) { await Disconnect(); throw new UserFriendlyException("Phiên kết nối nhà sáng tạo không còn đọc được. Hãy kết nối lại."); }
        catch (JsonException) { await Disconnect(); throw new UserFriendlyException("Phiên kết nối nhà sáng tạo không hợp lệ. Hãy kết nối lại."); }
        if (tokens is null || tokens.RefreshExpiresAt <= DateTimeOffset.UtcNow)
        { await Disconnect(); return null; }
        if (tokens.AccessExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(1))
        {
            var refreshed = await _client.Refresh(tokens.RefreshToken);
            if (!string.Equals(refreshed.OpenId, tokens.OpenId, StringComparison.Ordinal))
                throw new UserFriendlyException("Danh tính nhà sáng tạo thay đổi khi làm mới mã truy cập. Hãy kết nối lại.");
            tokens = refreshed;
            await Save(tokens);
        }
        return await _client.GetProfile(tokens);
    }
    public async Task<string> GetLink(string productId)
    {
        if (!_user.IsAuthenticated || !IsConfigured) return null;
        var ciphertext = await _cache.GetStringAsync(Key);
        if (ciphertext is null) return null;
        TikTokCreatorTokens? tokens;
        try { tokens = JsonSerializer.Deserialize<TikTokCreatorTokens>(Protector.Unprotect(ciphertext)); }
        catch (CryptographicException) { await Disconnect(); throw new UserFriendlyException("Phiên kết nối nhà sáng tạo không còn đọc được. Hãy kết nối lại."); }
        catch (JsonException) { await Disconnect(); throw new UserFriendlyException("Phiên kết nối nhà sáng tạo không hợp lệ. Hãy kết nối lại."); }
        if (tokens is null || tokens.RefreshExpiresAt <= DateTimeOffset.UtcNow)
        { await Disconnect(); return null; }
        if (tokens.AccessExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(1))
        {
            var refreshed = await _client.Refresh(tokens.RefreshToken);
            if (!string.Equals(refreshed.OpenId, tokens.OpenId, StringComparison.Ordinal))
                throw new UserFriendlyException("Danh tính nhà sáng tạo thay đổi khi làm mới mã truy cập. Hãy kết nối lại.");
            tokens = refreshed;
            await Save(tokens);
        }
        return await _client.GetLink(tokens, productId);
    }

    public Task Disconnect() => _cache.RemoveAsync(Key);
    private Task Save(TikTokCreatorTokens tokens) => _cache.SetStringAsync(Key,
        Protector.Protect(JsonSerializer.Serialize(tokens)), new DistributedCacheEntryOptions
        { AbsoluteExpiration = tokens.RefreshExpiresAt < DateTimeOffset.UtcNow.AddHours(8)
            ? tokens.RefreshExpiresAt : DateTimeOffset.UtcNow.AddHours(8) });
}

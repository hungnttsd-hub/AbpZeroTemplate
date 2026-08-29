using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations.Shopee;
using WebHoanTien.Permissions;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Settings)]
[RemoteService(IsEnabled = false)]
public class AdminUserAffiliateIdAppService : WebHoanTienAppService, IAdminUserAffiliateIdAppService
{
    private readonly IRepository<UserAffiliateIdOverride, Guid> _overrides;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly IAffiliateIdResolver _affiliateIdResolver;
    private readonly ShopeeAffiliateLinkBuilder _linkBuilder;

    public AdminUserAffiliateIdAppService(IRepository<UserAffiliateIdOverride, Guid> overrides,
        IRepository<AffiliateTracking, Guid> trackings, IRepository<IdentityUser, Guid> users,
        IAffiliateIdResolver affiliateIdResolver, ShopeeAffiliateLinkBuilder linkBuilder)
    {
        _overrides = overrides;
        _trackings = trackings;
        _users = users;
        _affiliateIdResolver = affiliateIdResolver;
        _linkBuilder = linkBuilder;
    }

    public async Task<PagedResultDto<AdminUserAffiliateIdDto>> GetListAsync(AdminUserAffiliateIdListInput input)
    {
        var maxResultCount = Math.Clamp(input.MaxResultCount <= 0 ? 20 : input.MaxResultCount, 1, 100);
        var skipCount = Math.Max(input.SkipCount, 0);
        var overrides = await _overrides.GetListAsync();
        var userIds = overrides.Select(x => x.UserId).Distinct().ToList();
        var users = userIds.Count == 0
            ? new Dictionary<Guid, IdentityUser>()
            : (await _users.GetListAsync(x => userIds.Contains(x.Id))).ToDictionary(x => x.Id);

        IEnumerable<UserAffiliateIdOverride> filtered = overrides;
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var term = input.Filter.Trim();
            filtered = filtered.Where(x => x.AffiliateId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (users.TryGetValue(x.UserId, out var user) && GetEmail(user).Contains(term,
                    StringComparison.OrdinalIgnoreCase)));
        }

        var rows = filtered.OrderByDescending(x => x.LastModificationTime ?? x.CreationTime)
            .ThenByDescending(x => x.Id).ToList();
        return new PagedResultDto<AdminUserAffiliateIdDto>(rows.Count,
            rows.Skip(skipCount).Take(maxResultCount)
                .Select(x => Map(x, users.TryGetValue(x.UserId, out var user) ? GetEmail(user) : "Không xác định"))
                .ToList());
    }

    public async Task<ListResultDto<AdminAffiliateUserOptionDto>> GetUserOptionsAsync()
    {
        var users = await _users.GetListAsync(x => x.IsActive);
        return new ListResultDto<AdminAffiliateUserOptionDto>(users
            .Select(x => new AdminAffiliateUserOptionDto { UserId = x.Id, Email = GetEmail(x) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .OrderBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    public async Task<AdminUserAffiliateIdDto> SetAsync(SetUserAffiliateIdInput input)
    {
        if (input.Platform != AffiliatePlatform.Shopee)
            throw new UserFriendlyException("Hiện tại chỉ hỗ trợ Affiliate ID riêng cho Shopee.");

        var normalizedEmail = input.UserEmail.Trim().ToUpperInvariant();
        var user = (await _users.GetListAsync(x => x.NormalizedEmail == normalizedEmail && x.IsActive))
            .FirstOrDefault() ?? throw new UserFriendlyException(
                "Không tìm thấy tài khoản đang hoạt động với email này.",
                WebHoanTienDomainErrorCodes.AffiliateUserNotFound);
        var affiliateId = NormalizeAffiliateId(input.AffiliateId);
        var existing = (await _overrides.GetListAsync(x => x.UserId == user.Id && x.Platform == input.Platform))
            .SingleOrDefault();

        try
        {
            if (existing is null)
            {
                existing = new UserAffiliateIdOverride(GuidGenerator.Create(), user.Id, input.Platform, affiliateId,
                    input.AdminNote);
                await _overrides.InsertAsync(existing, autoSave: true);
            }
            else
            {
                existing.Change(affiliateId, input.AdminNote);
                await _overrides.UpdateAsync(existing, autoSave: true);
            }
        }
        catch (Exception exception) when (IsOverrideConflict(exception))
        {
            throw new UserFriendlyException(
                "Cấu hình Affiliate ID của tài khoản vừa được thay đổi ở một yêu cầu khác. Vui lòng thử lại.",
                WebHoanTienDomainErrorCodes.AffiliateIdOverrideConflict, innerException: exception);
        }

        await RebuildTrackingUrlsAsync(user.Id, input.Platform, affiliateId);
        return Map(existing, GetEmail(user));
    }

    public async Task RemoveAsync(Guid userId, AffiliatePlatform platform = AffiliatePlatform.Shopee)
    {
        if (platform != AffiliatePlatform.Shopee)
            throw new UserFriendlyException("Hiện tại chỉ hỗ trợ Affiliate ID riêng cho Shopee.");

        var existing = (await _overrides.GetListAsync(x => x.UserId == userId && x.Platform == platform))
            .SingleOrDefault();
        if (existing is null) return;

        await _overrides.DeleteAsync(existing, autoSave: true);
        var fallback = await _affiliateIdResolver.ResolveAsync(userId, platform);
        await RebuildTrackingUrlsAsync(userId, platform, fallback.AffiliateId);
    }

    private async Task RebuildTrackingUrlsAsync(Guid userId, AffiliatePlatform platform, string affiliateId)
    {
        var trackings = await _trackings.GetListAsync(x => x.UserId == userId && x.Platform == platform &&
            x.Status == AffiliateTrackingStatus.Active);
        foreach (var tracking in trackings)
            tracking.SetAffiliateLink(_linkBuilder.Build(tracking.NormalizedUrl, tracking.TrackingToken, affiliateId));
        if (trackings.Count > 0) await _trackings.UpdateManyAsync(trackings, autoSave: true);
    }

    private static string NormalizeAffiliateId(string affiliateId)
    {
        try
        {
            return AffiliateIdRules.Normalize(affiliateId);
        }
        catch (ArgumentException exception)
        {
            throw new UserFriendlyException(exception.Message);
        }
    }

    private static string GetEmail(IdentityUser user) => user.Email ?? user.UserName;

    private static bool IsOverrideConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            if (current.Message.Contains("IX_UserAffiliateIdOverride_UserId_Platform",
                    StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static AdminUserAffiliateIdDto Map(UserAffiliateIdOverride entity, string email) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        UserEmail = email,
        Platform = entity.Platform,
        AffiliateId = entity.AffiliateId,
        AdminNote = entity.AdminNote,
        CreationTime = entity.CreationTime,
        CreatorId = entity.CreatorId,
        LastModificationTime = entity.LastModificationTime,
        LastModifierId = entity.LastModifierId,
        IsDeleted = entity.IsDeleted,
        DeleterId = entity.DeleterId,
        DeletionTime = entity.DeletionTime
    };
}

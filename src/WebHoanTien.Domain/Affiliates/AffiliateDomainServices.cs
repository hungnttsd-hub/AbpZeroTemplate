using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace WebHoanTien.Affiliates;

public interface ITrackingTokenGenerator
{
    string Create();
}

public class TrackingTokenGenerator : ITrackingTokenGenerator, ITransientDependency
{
    public string Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

public interface IAffiliateUrlNormalizer
{
    bool TryNormalize(string input, out string normalizedUrl, out string? itemId);
}

[ExposeServices(typeof(IAffiliateUrlNormalizer), typeof(ShopeeUrlNormalizer))]
public class ShopeeUrlNormalizer : IAffiliateUrlNormalizer, ITransientDependency
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "shopee.vn", "www.shopee.vn", "s.shopee.vn", "shope.ee"
    };

    public bool TryNormalize(string input, out string normalizedUrl, out string? itemId)
    {
        normalizedUrl = string.Empty;
        itemId = null;
        if (string.IsNullOrWhiteSpace(input) || input.Length > WebHoanTienConsts.UrlMaxLength ||
            !Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !AllowedHosts.Contains(uri.IdnHost) || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var builder = new UriBuilder(uri) { Fragment = string.Empty, Host = uri.IdnHost.ToLowerInvariant(), Port = -1 };
        if (!IsShortHost(uri.IdnHost))
        {
            builder.Query = NormalizeQuery(uri.Query);
            itemId = ExtractItemId(uri.AbsolutePath);
        }

        normalizedUrl = builder.Uri.AbsoluteUri.TrimEnd('/');
        return true;
    }

    public static bool IsShortHost(string host) => host.Equals("s.shopee.vn", StringComparison.OrdinalIgnoreCase) || host.Equals("shope.ee", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return string.Empty;
        var kept = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', 2))
            .Where(x => !x[0].StartsWith("utm_", StringComparison.OrdinalIgnoreCase) &&
                        !x[0].Equals("sp_atk", StringComparison.OrdinalIgnoreCase) &&
                        !x[0].Equals("xptdk", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x[0], StringComparer.Ordinal)
            .Select(x => x.Length == 1 ? x[0] : $"{x[0]}={x[1]}");
        return string.Join('&', kept);
    }

    private static string? ExtractItemId(string path)
    {
        var match = System.Text.RegularExpressions.Regex.Match(path, @"-i\.\d+\.(\d+)(?:/|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
            match = System.Text.RegularExpressions.Regex.Match(path, @"/product/\d+/(\d+)(?:/|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}

public sealed record CommissionAllocationInput(string Key, decimal Weight);
public sealed record CommissionAllocation(string Key, decimal NetCommission, decimal UserCommission);

public class AffiliateCommissionCalculator : ITransientDependency
{
    public decimal CalculateUserCommission(decimal netCommission, decimal userShareRate) =>
        decimal.Round(netCommission * userShareRate / 100m, 0, MidpointRounding.AwayFromZero);

    public IReadOnlyList<CommissionAllocation> Allocate(decimal netCommission, decimal userShareRate, IEnumerable<CommissionAllocationInput> inputs)
    {
        var rows = inputs.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
        if (rows.Count == 0) return Array.Empty<CommissionAllocation>();

        var totalWeight = rows.Sum(x => Math.Max(0m, x.Weight));
        var targetUser = CalculateUserCommission(netCommission, userShareRate);
        decimal allocatedNet = 0m;
        decimal allocatedUser = 0m;
        var result = new List<CommissionAllocation>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
        {
            var last = index == rows.Count - 1;
            var ratio = totalWeight == 0m ? 1m / rows.Count : Math.Max(0m, rows[index].Weight) / totalWeight;
            var itemNet = last ? netCommission - allocatedNet : decimal.Round(netCommission * ratio, 0, MidpointRounding.AwayFromZero);
            var itemUser = last ? targetUser - allocatedUser : decimal.Round(targetUser * ratio, 0, MidpointRounding.AwayFromZero);
            allocatedNet += itemNet;
            allocatedUser += itemUser;
            result.Add(new CommissionAllocation(rows[index].Key, itemNet, itemUser));
        }

        return result;
    }
}

public class AffiliateCommissionRuleManager : DomainService
{
    private readonly IRepository<AffiliateCommissionRule, Guid> _repository;

    public AffiliateCommissionRuleManager(IRepository<AffiliateCommissionRule, Guid> repository)
    {
        _repository = repository;
    }

    public async Task EnsureNoOverlapAsync(AffiliatePlatform platform, DateTime from, DateTime? to, Guid? excludingId = null)
    {
        var rules = await _repository.GetListAsync(x => x.Platform == platform && x.IsActive && (!excludingId.HasValue || x.Id != excludingId));
        if (rules.Any(x => x.Overlaps(from, to)))
        {
            throw new BusinessException(WebHoanTienDomainErrorCodes.CommissionRuleOverlap);
        }
    }

    public async Task<AffiliateCommissionRule> GetForPurchaseAsync(AffiliatePlatform platform, DateTime purchaseTime)
    {
        var rules = await _repository.GetListAsync(x => x.Platform == platform && x.IsActive);
        var rule = rules.Where(x => x.AppliesAt(purchaseTime)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        return rule ?? throw new BusinessException(WebHoanTienDomainErrorCodes.CommissionRuleNotFound);
    }
}

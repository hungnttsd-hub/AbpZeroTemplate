using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Timing;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Affiliates;

public class TikTokProductPreviewEnricher : ITransientDependency
{
    private readonly ITikTokCreatorApi _api;
    private readonly AffiliateUserShareRateResolver _rates;
    private readonly AffiliateCommissionCalculator _calculator;
    private readonly IClock _clock;
    private readonly ILogger<TikTokProductPreviewEnricher> _logger;

    public TikTokProductPreviewEnricher(ITikTokCreatorApi api, AffiliateUserShareRateResolver rates,
        AffiliateCommissionCalculator calculator, IClock clock, ILogger<TikTokProductPreviewEnricher> logger)
    { _api = api; _rates = rates; _calculator = calculator; _clock = clock; _logger = logger; }

    public async Task<IReadOnlyList<AffiliateTracking>> EnrichAsync(IEnumerable<AffiliateTracking> input,
        bool force = false, CancellationToken ct = default)
    {
        var links = input.Where(x => x.Platform == AffiliatePlatform.TikTok && x.ProductId is not null
            && x.GetProperty<string>(TikTokLinkAttribution.SourceProperty) == TikTokLinkAttribution.Source).ToList();
        var changed = new HashSet<AffiliateTracking>();
        var stale = links.Where(x => force || !x.GetProperty<DateTime?>(TikTokProductPreview.ReadAt).HasValue
            || x.GetProperty<DateTime?>(TikTokProductPreview.ReadAt) < _clock.Now.AddMinutes(-15)).ToList();
        foreach (var ids in stale.Select(x => x.ProductId!).Distinct(StringComparer.Ordinal).Chunk(50))
        {
            try
            {
                var products = await _api.GetProductsAsync(ids, ct);
                foreach (var link in stale.Where(x => ids.Contains(x.ProductId!) && !products.Any(p =>
                    p.ProductId == x.ProductId && p.CreatorOpenId == x.GetProperty<string>(TikTokLinkAttribution.CreatorProperty))))
                {
                    // A missing product must not retain a previous commission estimate.
                    link.SetProperty(TikTokProductPreview.CommissionMinimum, null);
                    link.SetProperty(TikTokProductPreview.CommissionMaximum, null);
                    link.SetProperty(TikTokProductPreview.ReadAt, _clock.Now);
                    changed.Add(link);
                }
                foreach (var product in products)
                foreach (var link in stale.Where(x => x.ProductId == product.ProductId
                    && x.GetProperty<string>(TikTokLinkAttribution.CreatorProperty) == product.CreatorOpenId))
                {
                    var image = Uri.TryCreate(product.ImageUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https"
                        && uri.UserInfo.Length == 0 && product.ImageUrl!.Length <= WebHoanTienConsts.UrlMaxLength
                        ? product.ImageUrl : null;
                    link.SetProduct(link.ProductId, link.ShopId,
                        string.IsNullOrWhiteSpace(product.Title) ? link.ProductName : product.Title[..Math.Min(500, product.Title.Length)],
                        image ?? link.ImageUrl, link.EstimatedCommission);
                    link.SetProperty(TikTokProductPreview.PriceMinimum, product.MinimumPrice);
                    link.SetProperty(TikTokProductPreview.PriceMaximum, product.MaximumPrice);
                    link.SetProperty(TikTokProductPreview.CommissionMinimum, product.MinimumCommission);
                    link.SetProperty(TikTokProductPreview.CommissionMaximum, product.MaximumCommission);
                    link.SetProperty(TikTokProductPreview.ReadAt, _clock.Now);
                    changed.Add(link);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogWarning("TikTok product preview unavailable: {ErrorType}", ex.GetType().Name); }
        }
        foreach (var group in links.GroupBy(x => x.UserId))
        {
            try
            {
                // The default matches the TikTok seed rule; preview calculation does not write a rule or credit a wallet.
                var rate = await _rates.GetForNextOrderAsync(group.Key, AffiliatePlatform.TikTok, _clock.Now,
                    WebHoanTienConsts.DefaultUserShareRate);
                foreach (var link in group)
                {
                    var min = link.GetProperty<decimal?>(TikTokProductPreview.CommissionMinimum);
                    var max = link.GetProperty<decimal?>(TikTokProductPreview.CommissionMaximum);
                    decimal? cashback = min.HasValue ? _calculator.CalculateUserCommission(min.Value, rate) : null;
                    decimal? cashbackMax = max.HasValue ? _calculator.CalculateUserCommission(max.Value, rate) : null;
                    if (link.EstimatedCommission == cashback && link.GetProperty<decimal?>(TikTokProductPreview.CashbackMaximum) == cashbackMax) continue;
                    link.SetProduct(link.ProductId, link.ShopId, link.ProductName, link.ImageUrl, cashback);
                    link.SetProperty(TikTokProductPreview.CashbackMaximum, cashbackMax);
                    changed.Add(link);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogWarning("TikTok cashback preview unavailable: {ErrorType}", ex.GetType().Name); }
        }
        return changed.ToList();
    }
}

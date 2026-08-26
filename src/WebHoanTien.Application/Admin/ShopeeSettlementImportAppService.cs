using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Validation;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations.Shopee;
using WebHoanTien.Permissions;
using WebHoanTien.Notifications;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Orders)]
public class ShopeeSettlementImportAppService : WebHoanTienAppService, IAdminShopeeSettlementImportAppService
{
    private readonly ShopeeSettlementReportParser _parser;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly AffiliateCommissionCalculator _calculator;
    private readonly CustomerNotificationManager _notificationManager;

    public ShopeeSettlementImportAppService(ShopeeSettlementReportParser parser,
        IRepository<AffiliateOrder, Guid> orders, IRepository<AffiliateConversion, Guid> conversions,
        AffiliateCommissionCalculator calculator, CustomerNotificationManager notificationManager)
    {
        _parser = parser;
        _orders = orders;
        _conversions = conversions;
        _calculator = calculator;
        _notificationManager = notificationManager;
    }

    [DisableValidation]
    public async Task<ShopeeSettlementImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default)
    {
        if (reportStream is null || !reportStream.CanRead)
            throw new UserFriendlyException("Không đọc được bảng kê thanh toán.",
                code: WebHoanTienDomainErrorCodes.InvalidShopeeSettlementReport);
        var extension = Path.GetExtension(reportFileName).ToLowerInvariant();
        if (extension is not ".csv" and not ".txt")
            throw new UserFriendlyException("Chỉ hỗ trợ bảng kê CSV hoặc TXT.",
                code: WebHoanTienDomainErrorCodes.InvalidShopeeSettlementReport);

        var parsed = await _parser.ParseAsync(reportStream, cancellationToken);
        var result = new ShopeeSettlementImportResultDto { ImportedRowCount = parsed.RowCount };
        var fallbackReference = Path.GetFileNameWithoutExtension(reportFileName);

        foreach (var row in parsed.Rows)
        {
            try
            {
                var matches = await _orders.GetListAsync(x => x.ExternalOrderId == row.ExternalOrderId,
                    cancellationToken: cancellationToken);
                if (matches.Count != 1)
                {
                    result.UnmatchedCount++;
                    if (result.Errors.Count < 20)
                        result.Errors.Add($"{row.ExternalOrderId}: không tìm thấy duy nhất một đơn hàng.");
                    continue;
                }

                var order = matches[0];
                if (order.Status == AffiliateOrderStatus.Settled)
                {
                    result.AlreadySettledCount++;
                    continue;
                }
                if (order.Status != AffiliateOrderStatus.Completed)
                    throw new BusinessException(WebHoanTienDomainErrorCodes.AffiliateOrderSettlementInvalidState)
                        .WithData("OrderId", row.ExternalOrderId);

                var conversion = await _conversions.GetAsync(order.ConversionId, cancellationToken: cancellationToken);
                var userCommission = _calculator.CalculateUserCommission(row.ActualPaidCommission,
                    conversion.UserShareRate);
                order.Settle(row.ActualPaidCommission, userCommission,
                    row.PaymentReference ?? fallbackReference, row.PaidAt ?? Clock.Now);
                await _orders.UpdateAsync(order, autoSave: true, cancellationToken: cancellationToken);
                if (conversion.UserId.HasValue)
                    await _notificationManager.NotifyOrderStatusAsync(conversion.UserId.Value, order);
                result.SettledCount++;
            }
            catch (Exception exception)
            {
                result.ErrorCount++;
                if (result.Errors.Count < 20) result.Errors.Add($"{row.ExternalOrderId}: {exception.Message}");
            }
        }

        return result;
    }
}

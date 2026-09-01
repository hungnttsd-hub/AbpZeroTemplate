using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Uow;
using Volo.Abp.Validation;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Orders)]
[RemoteService(IsEnabled = false)]
public class ShopeeSettlementImportAppService : WebHoanTienAppService, IAdminShopeeSettlementImportAppService
{
    private readonly ShopeeSettlementStagingService _staging;

    public ShopeeSettlementImportAppService(ShopeeSettlementStagingService staging) => _staging = staging;

    [UnitOfWork]
    [DisableValidation]
    public Task<ShopeeSettlementImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default) =>
        _staging.ImportAsync(reportStream, reportFileName, ShopeeSettlementImportSource.Manual,
            cancellationToken);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ShopeeAutomationSettlementImportAppService : WebHoanTienAppService,
    IShopeeAutomationSettlementImportAppService
{
    private readonly ShopeeSettlementStagingService _staging;

    public ShopeeAutomationSettlementImportAppService(ShopeeSettlementStagingService staging) =>
        _staging = staging;

    [UnitOfWork]
    [DisableValidation]
    public Task<ShopeeSettlementImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default) =>
        _staging.ImportAsync(reportStream, reportFileName, ShopeeSettlementImportSource.Automation,
            cancellationToken);
}

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Uow;
using Volo.Abp.Validation;
using WebHoanTien.Permissions;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Orders)]
public class ShopeeReportImportAppService : WebHoanTienAppService, IAdminShopeeReportImportAppService
{
    private readonly ShopeeReportImporter _importer;

    public ShopeeReportImportAppService(ShopeeReportImporter importer) => _importer = importer;

    [UnitOfWork]
    [DisableValidation]
    public async Task<ShopeeReportImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default)
        => await _importer.ImportAsync(reportStream, reportFileName, cancellationToken);
}

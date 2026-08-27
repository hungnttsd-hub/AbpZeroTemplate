using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Uow;
using Volo.Abp.Validation;

namespace WebHoanTien.Admin;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ShopeeAutomationImportAppService : WebHoanTienAppService, IShopeeAutomationImportAppService
{
    private readonly ShopeeReportImporter _importer;

    public ShopeeAutomationImportAppService(ShopeeReportImporter importer) => _importer = importer;

    [UnitOfWork]
    [DisableValidation]
    public Task<ShopeeReportImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default)
        => _importer.ImportAsync(reportStream, reportFileName, cancellationToken);
}

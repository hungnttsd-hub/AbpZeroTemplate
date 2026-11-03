using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebHoanTien.Affiliates;

public interface IShopeeSettlementApprovalLock
{
    Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default);
}

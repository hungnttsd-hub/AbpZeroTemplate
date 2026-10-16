using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebHoanTien.TikTokAffiliate;

public interface ITikTokSettlementLedgerLock
{
    Task LockAsync(Guid orderId, CancellationToken ct);
}

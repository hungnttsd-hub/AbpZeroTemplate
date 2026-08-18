using System;
using System.Threading.Tasks;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Operations;

[Serializable]
public sealed class AffiliateSyncJobArgs
{
    public AffiliatePlatform Platform { get; set; } = AffiliatePlatform.Shopee;
    public AffiliateSyncKind Kind { get; set; } = AffiliateSyncKind.Conversion;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public interface IAffiliateSyncCoordinator
{
    Task RequestPrioritySyncAsync(Guid userId);
    Task EnqueueSyncAsync(AffiliateSyncKind kind, DateTime? from = null, DateTime? to = null);
}

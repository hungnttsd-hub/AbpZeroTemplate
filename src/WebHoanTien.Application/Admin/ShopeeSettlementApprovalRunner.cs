using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Admin;

[UnitOfWork(IsDisabled = true)]
public class ShopeeSettlementApprovalRunner : ITransientDependency
{
    private readonly IShopeeSettlementApprovalLock _approvalLock;
    private readonly IUnitOfWorkManager _uow;
    public ShopeeSettlementApprovalRunner(IShopeeSettlementApprovalLock approvalLock, IUnitOfWorkManager uow)
    { _approvalLock = approvalLock; _uow = uow; }

    public async Task<T> RunAsync<T>(Func<Task<T>> approve)
    {
        // Hold the shared lock until the transaction commits, including manual approvals.
        await using var approvalLock = await _approvalLock.AcquireAsync();
        using var work = _uow.Begin(requiresNew: true, isTransactional: true);
        var result = await approve();
        await work.CompleteAsync();
        return result;
    }
}

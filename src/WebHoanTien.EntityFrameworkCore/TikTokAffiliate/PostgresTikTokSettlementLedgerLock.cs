using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using WebHoanTien.EntityFrameworkCore;

namespace WebHoanTien.TikTokAffiliate;

[ExposeServices(typeof(ITikTokSettlementLedgerLock))]
public class PostgresTikTokSettlementLedgerLock : ITikTokSettlementLedgerLock, ITransientDependency
{
    private readonly IDbContextProvider<WebHoanTienDbContext> _contexts;
    public PostgresTikTokSettlementLedgerLock(IDbContextProvider<WebHoanTienDbContext> contexts) => _contexts = contexts;

    public async Task LockAsync(Guid orderId, CancellationToken ct)
    {
        var db = await _contexts.GetDbContextAsync();
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Settlement requires a transaction.");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT "Id" FROM affiliate."Order" WHERE "Id" = {orderId} FOR UPDATE
            """, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT "Id" FROM affiliate."OrderItem" WHERE "OrderId" = {orderId} ORDER BY "Id" FOR UPDATE
            """, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT a."Id" FROM affiliate."OrderItemAttribution" a
            JOIN affiliate."OrderItem" i ON i."Id" = a."OrderItemId"
            WHERE i."OrderId" = {orderId} ORDER BY a."Id" FOR UPDATE OF a
            """, ct);
    }
}

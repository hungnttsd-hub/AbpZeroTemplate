using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Affiliates;

[ExposeServices(typeof(IShopeeSettlementApprovalLock))]
public class PostgresShopeeSettlementApprovalLock : IShopeeSettlementApprovalLock, ITransientDependency
{
    private readonly IConnectionStringResolver _connections;
    public PostgresShopeeSettlementApprovalLock(IConnectionStringResolver connections) => _connections = connections;

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        var builder = new NpgsqlConnectionStringBuilder(await _connections.ResolveAsync("Default"))
        { Pooling = false, Timeout = 10, CommandTimeout = 10 };
        var connection = new NpgsqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT pg_try_advisory_lock(hashtextextended('CatBack.Shopee.SettlementApproval.v1', 0))", connection);
            if (await command.ExecuteScalarAsync(cancellationToken) is not true)
                throw new UserFriendlyException("Đang có lượt duyệt đơn Shopee khác. Vui lòng đợi rồi thử lại.");
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.TikTokAffiliate;

[ExposeServices(typeof(ITikTokManualSyncLock))]
public class PostgresTikTokManualSyncLock : ITikTokManualSyncLock, ITransientDependency
{
    private readonly IConnectionStringResolver _connections;
    public PostgresTikTokManualSyncLock(IConnectionStringResolver connections) => _connections = connections;

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken ct = default)
    {
        var builder = new NpgsqlConnectionStringBuilder(await _connections.ResolveAsync("Default"))
        { Pooling = false, Timeout = 10, CommandTimeout = 10 };
        var connection = new NpgsqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(hashtextextended('CatBack.TikTokCreator.ManualOrders.v1', 0))", connection);
            if (await command.ExecuteScalarAsync(ct) is not true)
                throw new UserFriendlyException("Một admin hoặc tác vụ khác đang cập nhật đơn TikTok. Vui lòng đợi rồi thử lại.");
            // A dedicated, non-pooled connection owns the session lock. No transaction spans an API request.
            // Closing it (also after process failure) releases the PostgreSQL advisory lock.
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }
}

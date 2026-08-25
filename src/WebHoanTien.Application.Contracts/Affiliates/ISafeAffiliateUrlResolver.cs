using System.Threading;
using System.Threading.Tasks;

namespace WebHoanTien.Affiliates;

public interface ISafeAffiliateUrlResolver
{
    Task<(string NormalizedUrl, string? ItemId)> ResolveAsync(string input, CancellationToken cancellationToken = default);
}

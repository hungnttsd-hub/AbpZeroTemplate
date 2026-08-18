using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;

namespace WebHoanTien.Affiliates;

public static class AffiliateNetworkSafety
{
    public static async Task<IPAddress[]> ResolvePublicAddressesAsync(string host, CancellationToken cancellationToken = default)
    {
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(IsPrivateOrReserved))
            throw new BusinessException(WebHoanTienDomainErrorCodes.UnsafeRedirect).WithData("Host", host);
        return addresses;
    }

    public static bool IsPrivateOrReserved(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) return IsPrivateOrReserved(address.MapToIPv4());
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast) return true;

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return (bytes[0] & 0xfe) == 0xfc ||
                   bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8;
        }

        return bytes[0] is 0 or 10 or 127 ||
               bytes[0] == 100 && bytes[1] is >= 64 and <= 127 ||
               bytes[0] == 169 && bytes[1] == 254 ||
               bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
               bytes[0] == 192 && bytes[1] == 0 && bytes[2] is 0 or 2 ||
               bytes[0] == 192 && bytes[1] == 88 && bytes[2] == 99 ||
               bytes[0] == 192 && bytes[1] == 168 ||
               bytes[0] == 198 && bytes[1] is 18 or 19 ||
               bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100 ||
               bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113 ||
               bytes[0] >= 224;
    }
}

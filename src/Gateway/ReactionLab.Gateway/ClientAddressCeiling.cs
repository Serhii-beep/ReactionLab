using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ReactionLab.Gateway;

internal static class ClientAddressCeiling
{
    private const int RequestsPerSecond = 2000;
    private const int BurstRequests = 20000;
    private const int IPv6SubnetPrefixBytes = 8;

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = (context, _) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            }

            return ValueTask.CompletedTask;
        };

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(httpContext =>
            RateLimitPartition.GetTokenBucketLimiter(
                ClientNetwork(httpContext.Connection.RemoteIpAddress),
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = BurstRequests,
                    TokensPerPeriod = RequestsPerSecond,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    QueueLimit = 0
                }));
    }

    private static IPAddress ClientNetwork(IPAddress? address)
    {
        if (address is null)
        {
            return IPAddress.None;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            return address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address;
        }

        Span<byte> bytes = stackalloc byte[16];
        address.TryWriteBytes(bytes, out _);
        bytes[IPv6SubnetPrefixBytes..].Clear();

        return new IPAddress(bytes);
    }
}

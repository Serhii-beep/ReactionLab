using System.Net;
using System.Net.Sockets;

namespace ReactionLab.Identity.Api.Features.Sessions;

internal static class NetworkPrefixes
{
    private const int IPv4PrefixLength = 24;
    private const int IPv6PrefixLength = 48;
    private const int BitsPerByte = 8;
    private const int IPv4ByteCount = 4;

    private static readonly byte[] SixToFourPrefix = [0x20, 0x02];

    public static string? Of(string? address)
    {
        if (!IPAddress.TryParse(address, out var parsed))
        {
            return null;
        }

        var unwrapped = UnwrapIPv4(parsed);
        var prefixLength = unwrapped.AddressFamily == AddressFamily.InterNetwork ? IPv4PrefixLength : IPv6PrefixLength;
        var bytes = unwrapped.GetAddressBytes();

        Array.Clear(bytes, prefixLength / BitsPerByte, bytes.Length - prefixLength / BitsPerByte);

        return $"{new IPAddress(bytes)}/{prefixLength}";
    }

    private static IPAddress UnwrapIPv4(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();

        return address.AddressFamily == AddressFamily.InterNetworkV6 && bytes.AsSpan().StartsWith(SixToFourPrefix)
            ? new IPAddress(bytes.AsSpan(SixToFourPrefix.Length, IPv4ByteCount))
            : address;
    }
}

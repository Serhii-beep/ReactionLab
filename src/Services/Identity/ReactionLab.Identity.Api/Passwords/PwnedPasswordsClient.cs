using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace ReactionLab.Identity.Api.Passwords;

internal sealed class PwnedPasswordsClient(HttpClient httpClient)
{
    public const string Host = "api.pwnedpasswords.com";

    private const int HashPrefixLength = 5;

    private static readonly Uri RangeApi = new($"https://{Host}/range/");
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(1);

    public static void Configure(HttpClient httpClient)
    {
        httpClient.BaseAddress = RangeApi;
        httpClient.Timeout = CheckTimeout;
        httpClient.DefaultRequestHeaders.Add("Add-Padding", "true");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ReactionLab-Identity");
    }

    [SuppressMessage(
        "Security",
        "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "The Pwned Passwords range API is keyed by SHA-1, and only the hash's first 5 characters are sent.")]
    public async Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        var range = await httpClient.GetStringAsync(hash[..HashPrefixLength], cancellationToken);

        return IsListed(range, hash[HashPrefixLength..]);
    }

    private static bool IsListed(string range, string hashSuffix)
    {
        foreach (var line in range.AsSpan().EnumerateLines())
        {
            var separator = line.IndexOf(':');

            if (separator > 0
                && line[..separator].Equals(hashSuffix, StringComparison.OrdinalIgnoreCase)
                && !line[(separator + 1)..].Trim().SequenceEqual("0"))
            {
                return true;
            }
        }

        return false;
    }
}

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ReactionLab.Identity.Api.Http;

internal static class LocalReturnUrls
{
    public const string Default = "/";

    private const int MaximumLength = 512;

    public static bool TryNormalize(string? returnUrl, [NotNullWhen(true)] out string? normalized)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            normalized = Default;

            return true;
        }

        normalized = IsLocal(returnUrl) ? returnUrl : null;

        return normalized is not null;
    }

    public static string OrDefault(string? returnUrl) =>
        TryNormalize(returnUrl, out var normalized) ? normalized : Default;

    private static bool IsLocal(string returnUrl) =>
        returnUrl is ['/', ..]
        && returnUrl.Length <= MaximumLength
        && returnUrl.All(IsPrintableAscii)
        && RedirectHttpResult.IsLocalUrl(returnUrl);

    private static bool IsPrintableAscii(char character) => character is > ' ' and < '\u007f';
}

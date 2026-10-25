using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace ReactionLab.Identity.Api.Features.AccountSetups;

internal static class AccountSetupTokens
{
    public const int TokenLength = 43;

    private const int TokenBytes = 32;

    public static (string Token, byte[] TokenHash) Issue()
    {
        var token = RandomNumberGenerator.GetBytes(TokenBytes);

        return (Base64Url.EncodeToString(token), SHA256.HashData(token));
    }

    public static bool TryHash(string? token, [NotNullWhen(true)] out byte[]? tokenHash)
    {
        if (token is not { Length: TokenLength }
            || !Base64Url.IsValid(token, out var decodedLength)
            || decodedLength != TokenBytes)
        {
            tokenHash = null;

            return false;
        }

        tokenHash = SHA256.HashData(Base64Url.DecodeFromChars(token));

        return true;
    }
}

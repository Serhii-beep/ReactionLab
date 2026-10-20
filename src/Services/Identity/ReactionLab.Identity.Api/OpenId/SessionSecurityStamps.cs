using System.Security.Cryptography;
using System.Text;

namespace ReactionLab.Identity.Api.OpenId;

internal static class SessionSecurityStamps
{
    public static string Hash(string securityStamp) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp)));
}

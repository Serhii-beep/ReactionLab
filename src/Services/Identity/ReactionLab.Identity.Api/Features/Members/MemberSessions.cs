using System.Security.Cryptography;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Api.Features.Members;

internal static class MemberSessions
{
    private const int SecurityStampLength = 32;

    public static Task<int> EndAllAsync(AppDbContext context, Guid accountId) =>
        context.EndAccountSessionsAsync(accountId, RandomNumberGenerator.GetHexString(SecurityStampLength));
}

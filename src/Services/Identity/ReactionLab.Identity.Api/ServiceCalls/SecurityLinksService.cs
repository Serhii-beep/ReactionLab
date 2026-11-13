using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReactionLab.Identity.Api.Features;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.SecurityLinks;

namespace ReactionLab.Identity.Api.ServiceCalls;

internal sealed class SecurityLinksService(AppDbContext identityDatabase, ILogger<SecurityLinksService> logger)
    : SecurityLinks.SecurityLinksBase
{
    public override async Task<IssuedSecurityLink> IssueSecurityLink(
        IssueSecurityLinkRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId)
            || !Guid.TryParse(request.RequestId, out var requestId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The account and request ids are GUIDs."));
        }

        if (request.Purpose == SecurityLinkPurpose.Unspecified)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The request names no purpose."));
        }

        if (request.Purpose != SecurityLinkPurpose.EmailConfirmation)
        {
            throw new RpcException(
                new Status(StatusCode.Unimplemented, "This identity issues no link for that purpose."));
        }

        var (token, tokenHash) = OneTimeLinkTokens.Issue();
        var expiresAt = await identityDatabase.IssueEmailConfirmationLinkAsync(accountId, requestId, tokenHash)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "The request was replaced, used or expired."));

        logger.LogInformation("Issued an email confirmation link to account {AccountId}.", accountId);

        return new IssuedSecurityLink { Token = token, ExpiresAt = Timestamp.FromDateTimeOffset(expiresAt) };
    }
}

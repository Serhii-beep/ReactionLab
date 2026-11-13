using ReactionLab.Identity.Contracts.SecurityLinks.V1;

namespace ReactionLab.Notifications.ServiceCalls;

internal sealed class SecurityLinkIssuer(SecurityLinks.SecurityLinksClient securityLinks, IdentityCalls identity)
{
    public Task<IssuedSecurityLink?> IssueAsync(
        Guid accountId,
        Guid requestId,
        SecurityLinkPurpose purpose,
        CancellationToken cancellationToken) =>
        identity.CallAsync(
            options => securityLinks.IssueSecurityLinkAsync(
                new IssueSecurityLinkRequest
                {
                    AccountId = accountId.ToString(),
                    RequestId = requestId.ToString(),
                    Purpose = purpose
                },
                options),
            cancellationToken);
}

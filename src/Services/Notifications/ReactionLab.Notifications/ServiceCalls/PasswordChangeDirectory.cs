using ReactionLab.Identity.Contracts.Contacts.V1;

namespace ReactionLab.Notifications.ServiceCalls;

internal sealed class PasswordChangeDirectory(Contacts.ContactsClient contacts, IdentityCalls identity)
{
    public Task<PasswordChangeDetails?> FindAsync(Guid accountId, Guid changeId, CancellationToken cancellationToken) =>
        identity.CallAsync(
            options => contacts.GetPasswordChangeAsync(
                new GetPasswordChangeRequest { AccountId = accountId.ToString(), ChangeId = changeId.ToString() },
                options),
            cancellationToken);
}

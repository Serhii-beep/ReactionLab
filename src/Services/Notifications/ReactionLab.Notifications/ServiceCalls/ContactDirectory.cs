using ReactionLab.Identity.Contracts.Contacts.V1;

namespace ReactionLab.Notifications.ServiceCalls;

internal sealed class ContactDirectory(
    Contacts.ContactsClient contacts,
    IdentityCalls identity)
{
    public Task<Contact?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
        identity.CallAsync(
            options => contacts.GetContactAsync(new GetContactRequest { AccountId = accountId.ToString() }, options),
            cancellationToken);
}

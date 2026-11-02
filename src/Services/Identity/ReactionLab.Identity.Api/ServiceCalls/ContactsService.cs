using Grpc.Core;
using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.ServiceCalls;

internal sealed class ContactsService(AppDbContext identityDatabase) : Contacts.ContactsBase
{
    public override async Task<Contact> GetContact(GetContactRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The account id is not a GUID."));
        }

        var contact = await identityDatabase.FindContactAsync(accountId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "The account doesn't exist."));

        var reply = new Contact
        {
            AccountId = accountId.ToString(),
            EmailConfirmed = contact.EmailConfirmed,
            DisplayName = contact.DisplayName,
            Locale = contact.PreferredLocale
        };

        if (contact.Email is { } email)
        {
            reply.Email = email;
        }

        return reply;
    }
}

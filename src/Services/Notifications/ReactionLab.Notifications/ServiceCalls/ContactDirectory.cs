using Grpc.Core;
using ReactionLab.Identity.Contracts.Contacts.V1;

namespace ReactionLab.Notifications.ServiceCalls;

internal sealed class ContactDirectory(
    Contacts.ContactsClient contacts,
    ServiceTokenSource tokens,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(2);

    public async Task<Contact?> FindAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var token = await tokens.GetAsync(cancellationToken);
        var credentials = CallCredentials.FromInterceptor((_, metadata) =>
        {
            metadata.Add("Authorization", $"Bearer {token}");

            return Task.CompletedTask;
        });

        try
        {
            return await contacts.GetContactAsync(
                new GetContactRequest { AccountId = accountId.ToString() },
                new CallOptions(
                    deadline: (timeProvider.GetUtcNow() + Deadline).UtcDateTime,
                    cancellationToken: cancellationToken,
                    credentials: credentials));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            tokens.Forget(token);
            throw;
        }
        catch (RpcException ex)
            when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }
}

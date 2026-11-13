using Grpc.Core;

namespace ReactionLab.Notifications.ServiceCalls;

internal sealed class IdentityCalls(ServiceTokenSource tokens, TimeProvider timeProvider)
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(2);

    public async Task<TReply?> CallAsync<TReply>(
        Func<CallOptions, AsyncUnaryCall<TReply>> call,
        CancellationToken cancellationToken)
        where TReply : class
    {
        var token = await tokens.GetAsync(cancellationToken);
        var credentials = CallCredentials.FromInterceptor((_, metadata) =>
        {
            metadata.Add("Authorization", $"Bearer {token}");

            return Task.CompletedTask;
        });

        try
        {
            return await call(new CallOptions(
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

using OpenIddict.Abstractions;
using OpenIddict.Client;
using ReactionLab.Identity.Contracts;

namespace ReactionLab.Notifications.ServiceCalls;

internal sealed class ServiceTokenSource(OpenIddictClientService openIdClient, TimeProvider timeProvider)
    : IDisposable
{
    private static readonly TimeSpan RenewalMargin = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _renewal = new(1, 1);
    private ServiceToken? _current;

    public async ValueTask<string> GetAsync(CancellationToken cancellationToken)
    {
        if (FindUsableToken() is { } token)
        {
            return token;
        }

        await _renewal.WaitAsync(cancellationToken);

        try
        {
            if (FindUsableToken() is { } renewedToken)
            {
                return renewedToken;
            }

            var renewed = await RequestTokenAsync(cancellationToken);

            Volatile.Write(ref _current, renewed);

            return renewed.Value;
        }
        finally
        {
            _renewal.Release();
        }
    }

    public void Forget(string rejectedToken)
    {
        var current = Volatile.Read(ref _current);

        if (current?.Value == rejectedToken)
        {
            Interlocked.CompareExchange(ref _current, null, current);
        }
    }

    public void Dispose() => _renewal.Dispose();

    private async Task<ServiceToken> RequestTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await openIdClient.AuthenticateWithClientCredentialsAsync(new()
            {
                Scopes = [IdentityScopes.Contacts],
                CancellationToken = cancellationToken
            });

            return new ServiceToken(result.AccessToken, result.AccessTokenExpirationDate ?? timeProvider.GetUtcNow());
        }
        catch (OpenIddictExceptions.ProtocolException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private string? FindUsableToken() =>
        Volatile.Read(ref _current) is { } token && token.ExpiresAt - RenewalMargin > timeProvider.GetUtcNow()
            ? token.Value
            : null;
}

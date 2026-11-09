using System.Net.Sockets;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ReactionLab.Notifications.Sending;

internal sealed class SmtpMailer(SmtpEndpoint endpoint) : IAsyncDisposable
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan QuitTimeout = TimeSpan.FromSeconds(5);

    private readonly SmtpClient _client = new()
    {
        Timeout = (int)OperationTimeout.TotalMilliseconds,
        LocalDomain = endpoint.LocalDomain
    };

    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        var reusesConnection = _client.IsConnected;

        try
        {
            await SendOnceAsync(message, cancellationToken);
        }
        catch (Exception ex) when (reusesConnection
            && ex is IOException or SmtpProtocolException or ServiceNotConnectedException
                or SmtpCommandException { StatusCode: SmtpStatusCode.ServiceNotAvailable })
        {
            await SendOnceAsync(message, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await CloseAsync(quit: true);
        }
        finally
        {
            _client.Dispose();
        }
    }

    private async Task SendOnceAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        try
        {
            if (!_client.IsConnected)
            {
                await _client.ConnectAsync(endpoint.Host, endpoint.Port, endpoint.Security, cancellationToken);

                if (endpoint.Credentials is { } credentials)
                {
                    await _client.AuthenticateAsync(credentials, cancellationToken);
                }
            }

            await _client.SendAsync(message, cancellationToken);
        }
        catch (SslHandshakeException ex)
            when (ex.InnerException is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            await CloseAsync(quit: false);
            throw new OperationCanceledException(cancellationToken);
        }
        catch
        {
            await CloseAsync(quit: false);
            throw;
        }
    }

    private async Task CloseAsync(bool quit)
    {
        using var timeout = new CancellationTokenSource(QuitTimeout);

        try
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync(quit, timeout.Token);
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or TimeoutException or SmtpProtocolException
            or OperationCanceledException or ObjectDisposedException)
        {

        }
    }
}

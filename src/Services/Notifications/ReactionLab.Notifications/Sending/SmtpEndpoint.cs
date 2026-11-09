using System.Data.Common;
using System.Net;
using MailKit.Security;

namespace ReactionLab.Notifications.Sending;

internal sealed class SmtpEndpoint
{
    private const int SubmissionPort = 587;
    private const int ImplicitTlsPort = 465;

    private SmtpEndpoint(
        string host,
        int port,
        SecureSocketOptions security,
        NetworkCredential? credentials,
        string localDomain)
    {
        Host = host;
        Port = port;
        Security = security;
        Credentials = credentials;
        LocalDomain = localDomain;
    }

    public string Host { get; }

    public int Port { get; }

    public SecureSocketOptions Security { get; }

    public NetworkCredential? Credentials { get; }

    public string LocalDomain { get; }

    public static SmtpEndpoint Parse(string connectionString, MailOptions options)
    {
        var settings = new DbConnectionStringBuilder { ConnectionString = connectionString };

        if (!settings.TryGetValue("Endpoint", out var endpoint)
            || !Uri.TryCreate(endpoint as string, UriKind.Absolute, out var address)
            || address.UserInfo.Length > 0
            || address.PathAndQuery != "/"
            || address.Fragment.Length > 0)
        {
            throw new InvalidOperationException(
                "The mail connection string needs an Endpoint such as smtp://host:587, with no user, path or query.");
        }

        var (security, defaultPort) = address.Scheme switch
        {
            "smtps" => (SecureSocketOptions.SslOnConnect, ImplicitTlsPort),
            "smtp" => (options.RequireTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, SubmissionPort),
            _ => throw new InvalidOperationException("The mail Endpoint's scheme must be smtp or smtps.")
        };

        var credentials = ReadCredentials(settings);

        if (credentials is not null && security == SecureSocketOptions.None)
        {
            throw new InvalidOperationException("Mail credentials are only sent over TLS.");
        }

        return new SmtpEndpoint(
            address.IdnHost,
            address.IsDefaultPort ? defaultPort : address.Port,
            security,
            credentials,
            options.FromAddress[(options.FromAddress.LastIndexOf('@') + 1)..]);
    }

    private static NetworkCredential? ReadCredentials(DbConnectionStringBuilder settings)
    {
        if (!settings.TryGetValue("Username", out var username) || username is not string { Length: > 0 } name)
        {
            return null;
        }

        var password = settings.TryGetValue("Password", out var value) ? value as string : null;

        return new NetworkCredential(name, password);
    }
}

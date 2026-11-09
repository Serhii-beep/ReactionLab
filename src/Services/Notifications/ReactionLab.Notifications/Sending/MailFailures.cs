using System.Net.Sockets;
using System.Text.RegularExpressions;
using Grpc.Core;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using OpenIddict.Abstractions;

namespace ReactionLab.Notifications.Sending;

internal static partial class MailFailures
{
    public const string AttemptsExhausted = "AttemptsExhausted";

    private const int FirstPermanentSmtpStatus = 500;

    public static MailFailureKind Classify(Exception exception) =>
        exception switch
        {
            MailCompositionException or KeyNotFoundException => MailFailureKind.Permanent,
            RpcException { StatusCode: StatusCode.InvalidArgument } => MailFailureKind.Permanent,
            SmtpCommandException reply => ClassifyReply(reply),
            RpcException or OpenIddictExceptions.ProtocolException or IOException or SocketException
                or TimeoutException or OperationCanceledException or SmtpProtocolException
                or SslHandshakeException or AuthenticationException or ServiceNotConnectedException
                or ServiceNotAuthenticatedException or NotSupportedException
                => MailFailureKind.Systemic,
            _ => MailFailureKind.Transient
        };

    public static string Describe(Exception exception) =>
        exception switch
        {
            RpcException rpc => $"{nameof(RpcException)} {rpc.StatusCode}",
            SmtpCommandException reply => DescribeReply(reply),
            MailCompositionException { InnerException: { } inner } =>
                $"{nameof(MailCompositionException)} {inner.GetType().Name}",
            _ => exception.GetType().Name
        };

    public static bool MayQuoteAddress(Exception exception) =>
        exception is RpcException
        || exception.GetType().Assembly == typeof(SmtpClient).Assembly
        || exception.GetType().Assembly == typeof(MimeMessage).Assembly;

    private static MailFailureKind ClassifyReply(SmtpCommandException reply)
    {
        if (!IsAboutTheRecipientAddress(reply))
        {
            return MailFailureKind.Systemic;
        }

        return (int)reply.StatusCode >= FirstPermanentSmtpStatus
            ? MailFailureKind.Permanent
            : MailFailureKind.Transient;
    }

    private static bool IsAboutTheRecipientAddress(SmtpCommandException reply) =>
        reply.ErrorCode is SmtpErrorCode.RecipientNotAccepted or SmtpErrorCode.MessageNotAccepted
        && EnhancedStatusCode().Match(reply.Message) is { Success: true } code
        && code.Groups["subject"].Value == "1"
        && code.Groups["detail"].Value is "1" or "2" or "3" or "4" or "6" or "10";

    private static string DescribeReply(SmtpCommandException reply)
    {
        var description = $"{nameof(SmtpCommandException)} {reply.ErrorCode} {(int)reply.StatusCode}";

        return EnhancedStatusCode().Match(reply.Message) is { Success: true } code
            ? $"{description} {code.Value.Trim()}"
            : description;
    }

    [GeneratedRegex(@"^\s*[245]\.(?<subject>\d{1,3})\.(?<detail>\d{1,3})(?=\s|$)")]
    private static partial Regex EnhancedStatusCode();
}

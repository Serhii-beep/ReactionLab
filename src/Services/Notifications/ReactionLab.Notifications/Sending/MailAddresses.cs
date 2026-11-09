using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;
using MimeKit;

namespace ReactionLab.Notifications.Sending;

internal static class MailAddresses
{
    private const string AceLabelPrefix = "xn--";

    private static readonly SearchValues<char> RewrittenAddressCharacters = SearchValues.Create("\"()[]\\");

    public static bool TryCreateMailbox(
        string? displayName,
        string address,
        [NotNullWhen(true)] out MailboxAddress? mailbox)
    {
        mailbox = null;
        var at = address.LastIndexOf('@');

        if (at <= 0
            || address.AsSpan().ContainsAny(RewrittenAddressCharacters)
            || !TryEncodeDomain(address[(at + 1)..], out var encodedDomain)
            || !IsPublicHostName(encodedDomain))
        {
            return false;
        }

        try
        {
            mailbox = new MailboxAddress(displayName ?? string.Empty, address);
        }
        catch (ParseException)
        {
            return false;
        }

        return string.Equals(
            mailbox.GetAddress(idnEncode: true),
            $"{address[..at]}@{encodedDomain}",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryEncodeDomain(string domain, out string encodedDomain)
    {
        var idn = new IdnMapping();

        try
        {
            encodedDomain = idn.GetAscii(domain);

            return Ascii.IsValid(domain)
                || string.Equals(idn.GetUnicode(encodedDomain), domain, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            encodedDomain = string.Empty;

            return false;
        }
    }

    private static bool IsPublicHostName(string encodedDomain)
    {
        var lastDot = encodedDomain.LastIndexOf('.');
        var topLevel = encodedDomain[(lastDot + 1)..];

        return lastDot > 0
            && topLevel.Length >= 2
            && !IPAddress.TryParse(encodedDomain, out _)
            && !topLevel.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && (topLevel.All(char.IsAsciiLetter)
                || topLevel.StartsWith(AceLabelPrefix, StringComparison.OrdinalIgnoreCase));
    }
}

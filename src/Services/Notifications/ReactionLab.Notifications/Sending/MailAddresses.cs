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

    private static readonly HashSet<string> SpecialUseTopLevelDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "alt", "arpa", "corp", "example", "home", "internal", "intranet", "invalid", "lan", "local", "localdomain",
        "localhost", "mail", "onion", "private", "test"
    };

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

    public static void RequireIdnaSupport()
    {
        try
        {
            var mapping = new IdnMapping();

            if (mapping.GetAscii("\uFF45xample.com") == "example.com"
                && mapping.GetAscii("stra\u00DFe.de") == "xn--strae-oqa.de")
            {
                return;
            }
        }
        catch (ArgumentException)
        {
        }

        throw new InvalidOperationException(
            "Mail addresses need ICU's IDNA mapping. Run with ICU globalization, not in invariant or NLS mode.");
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
            && !SpecialUseTopLevelDomains.Contains(topLevel)
            && (topLevel.All(char.IsAsciiLetter)
                || topLevel.StartsWith(AceLabelPrefix, StringComparison.OrdinalIgnoreCase));
    }
}

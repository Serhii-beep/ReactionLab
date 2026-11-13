using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;
using MimeKit;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class EmailAddresses
{
    private const string AceLabelPrefix = "xn--";
    private const int MaximumLocalPartBytes = 64;

    private static readonly SearchValues<char> AddressSyntaxCharacters = SearchValues.Create("\"()[]\\");

    private static readonly HashSet<string> SpecialUseTopLevelDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "alt", "arpa", "corp", "example", "home", "internal", "intranet", "invalid", "lan", "local", "localdomain",
        "localhost", "mail", "onion", "private", "test"
    };

    public static bool TryCanonicalize(string address, [NotNullWhen(true)] out string? canonical)
    {
        canonical = null;
        var at = address.LastIndexOf('@');

        if (at <= 0
            || address.AsSpan().ContainsAny(AddressSyntaxCharacters)
            || address[..at].EnumerateRunes().Any(IsInvisible)
            || Encoding.UTF8.GetByteCount(address.AsSpan(0, at)) > MaximumLocalPartBytes
            || !TryMapDomain(address[(at + 1)..], out var asciiDomain, out var unicodeDomain)
            || unicodeDomain.EnumerateRunes().Any(IsInvisible)
            || !IsPublicHostDomain(asciiDomain))
        {
            return false;
        }

        var localPart = address[..at];
        var mapped = $"{localPart}@{unicodeDomain}";

        try
        {
            var mailbox = new MailboxAddress(string.Empty, mapped);

            if (!string.Equals(
                mailbox.GetAddress(idnEncode: true),
                $"{localPart}@{asciiDomain}",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        catch (ParseException)
        {
            return false;
        }

        canonical = mapped;

        return true;
    }

    public static void RequireIdnaSupport()
    {
        if (!TryMapDomain("\uFF45xample.com", out var fullWidth, out _) || fullWidth != "example.com"
            || !TryMapDomain("stra\u00DFe.de", out var sharpS, out _) || sharpS != "xn--strae-oqa.de")
        {
            throw new InvalidOperationException("Email addresses need ICU's IDNA mapping.");
        }
    }

    private static bool IsInvisible(Rune character) =>
        DisplayNames.BlankLookingCharacters.Contains(character.Value)
        || character.Value is >= 0x180B and <= 0x180F or >= 0xFE00 and <= 0xFE0F or >= 0xE0100 and <= 0xE01EF
        || Rune.GetUnicodeCategory(character) is UnicodeCategory.Format
            or UnicodeCategory.Control
            or UnicodeCategory.SpaceSeparator
            or UnicodeCategory.LineSeparator
            or UnicodeCategory.ParagraphSeparator
            or UnicodeCategory.PrivateUse
            or UnicodeCategory.OtherNotAssigned;

    private static bool TryMapDomain(string domain, out string asciiDomain, out string unicodeDomain)
    {
        var mapping = new IdnMapping { UseStd3AsciiRules = true };

        try
        {
            asciiDomain = mapping.GetAscii(domain);
            unicodeDomain = mapping.GetUnicode(asciiDomain);

            return true;
        }
        catch (ArgumentException)
        {
            asciiDomain = string.Empty;
            unicodeDomain = string.Empty;

            return false;
        }
    }

    private static bool IsPublicHostDomain(string asciiDomain)
    {
        var lastDot = asciiDomain.LastIndexOf('.');
        var topLevel = asciiDomain[(lastDot + 1)..];

        return lastDot > 0
            && topLevel.Length >= 2
            && !IPAddress.TryParse(asciiDomain, out _)
            && !SpecialUseTopLevelDomains.Contains(topLevel)
            && (topLevel.All(char.IsAsciiLetter)
                || topLevel.StartsWith(AceLabelPrefix, StringComparison.OrdinalIgnoreCase));
    }
}

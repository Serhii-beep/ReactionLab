using System.Globalization;
using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;
using ReactionLab.SharedKernel;

namespace ReactionLab.Notifications.Templates;

internal sealed class PasswordChangedMail(ITranslationCatalog catalog, IOptions<MailOptions> options) : IMailTemplate
{
    private const string ForgotPasswordPath = "account/forgot-password";
    private const string WhenFormat = "dddd, d MMMM, HH:mm";

    public string MailType => MailTypes.PasswordChanged;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public SecurityLinkPurpose? LinkPurpose => null;

    public bool DescribesPasswordChange => true;

    public MailContent Compose(MailRecipient recipient)
    {
        var change = recipient.PasswordChange
            ?? throw new InvalidOperationException("A password-changed notice needs its change.");
        var locale = recipient.Locale;
        var title = catalog.Translate(locale, "passwordChanged.title");
        var greeting = recipient.DisplayName is { } displayName
            ? catalog.Phrase(locale, "common.greeting", new MailArgument("name", displayName))
            : catalog.Phrase(locale, "common.greetingWithoutName");

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(greeting),
                new MailParagraph(catalog.Phrase(locale, "passwordChanged.changed")),
                new MailFacts(
                [
                    new MailFact(
                        catalog.Translate(locale, "passwordChanged.when"),
                        DescribeTime(change.ChangedAt, recipient.TimeZone, locale)),
                    new MailFact(catalog.Translate(locale, "passwordChanged.device"), DescribeDevice(change, locale))
                ]),
                new MailParagraph(catalog.Phrase(locale, "passwordChanged.ifItWasYou")),
                new MailParagraph(catalog.Phrase(locale, "passwordChanged.ifItWasNotYou")),
                new MailButton(
                    catalog.Translate(locale, "passwordChanged.reset"),
                    new Uri(options.Value.SiteUri!, ForgotPasswordPath))
            ],
            [
                catalog.Translate(locale, "passwordChanged.reason"),
                catalog.Translate(locale, "common.tagline")
            ]);
    }

    private static string DescribeTime(DateTimeOffset changedAt, string? timeZone, string locale)
    {
        var local = TimeZoneInfo.ConvertTime(changedAt, IanaTimeZones.Find(timeZone) ?? TimeZoneInfo.Utc);

        return $"{local.ToString(WhenFormat, CultureInfo.GetCultureInfo(locale))} {GmtOffset(local.Offset)}";
    }

    private static string GmtOffset(TimeSpan offset)
    {
        if (offset == TimeSpan.Zero)
        {
            return "GMT";
        }

        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var hours = Math.Abs(offset.Hours);
        var minutes = Math.Abs(offset.Minutes);

        return minutes == 0
            ? string.Create(CultureInfo.InvariantCulture, $"GMT{sign}{hours}")
            : string.Create(CultureInfo.InvariantCulture, $"GMT{sign}{hours}:{minutes:00}");
    }

    private string DescribeDevice(RecordedPasswordChange change, string locale) =>
        (change.Browser, change.OperatingSystem) switch
        {
            ("", "") => catalog.Translate(locale, "passwordChanged.unknownDevice"),
            ("", var system) => system,
            (var browser, "") => browser,
            var (browser, system) => catalog.Translate(
                locale,
                "passwordChanged.deviceDescription",
                new Dictionary<string, object?> { ["browser"] = browser, ["system"] = system })
        };
}

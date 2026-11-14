using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class PasswordChangedMail(ITranslationCatalog catalog, IOptions<MailOptions> options) : IMailTemplate
{
    private const string ForgotPasswordPath = "account/forgot-password";

    public string MailType => MailTypes.PasswordChanged;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public SecurityLinkPurpose? LinkPurpose => null;

    public MailDetailKind Detail => MailDetailKind.PasswordChange;

    public MailContent Compose(MailRecipient recipient)
    {
        var change = recipient.Detail as RecordedPasswordChange
            ?? throw new InvalidOperationException("A password-changed notice needs its change.");
        var locale = recipient.Locale;
        var title = catalog.Translate(locale, "passwordChanged.title");

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(MailGreetings.For(catalog, recipient)),
                new MailParagraph(catalog.Phrase(locale, "passwordChanged.changed")),
                new MailFacts(
                [
                    new MailFact(
                        catalog.Translate(locale, "common.when"),
                        MailTimes.DescribeMoment(change.ChangedAt, recipient.TimeZone, locale)),
                    new MailFact(
                        catalog.Translate(locale, "common.device"),
                        MailDevices.Describe(catalog, locale, change.Browser, change.OperatingSystem))
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
}

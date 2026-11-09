using Microsoft.Extensions.Options;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class RepeatedRegistrationMail(ITranslationCatalog catalog, IOptions<MailOptions> options)
    : IMailTemplate
{
    private const string SignInPath = "sign-in";

    public string MailType => MailTypes.RepeatedRegistration;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public MailContent Compose(MailRecipient recipient)
    {
        var locale = recipient.Locale;
        var title = catalog.Translate(locale, "repeatedRegistration.title");
        var greeting = recipient.DisplayName is { } displayName
            ? catalog.Phrase(locale, "common.greeting", new MailArgument("name", displayName))
            : catalog.Phrase(locale, "common.greetingWithoutName");

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(greeting),
                new MailParagraph(catalog.Phrase(
                    locale,
                    "repeatedRegistration.attempt",
                    new MailArgument("email", recipient.Email, IsEmphasized: true))),
                new MailParagraph(catalog.Phrase(locale, "repeatedRegistration.ifItWasYou")),
                new MailButton(
                    catalog.Translate(locale, "repeatedRegistration.signIn"),
                    new Uri(options.Value.SiteUri!, SignInPath)),
                new MailParagraph(catalog.Phrase(locale, "repeatedRegistration.ifItWasNotYou"), IsAside: true)
            ],
            [
                catalog.Translate(locale, "common.securityNoticeReason"),
                catalog.Translate(locale, "common.tagline")
            ]);
    }
}

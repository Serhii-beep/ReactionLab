using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class EmailConfirmationMail(
    ITranslationCatalog catalog,
    IOptions<MailOptions> options,
    TimeProvider timeProvider) : IMailTemplate
{
    private const string ConfirmationPath = "account/confirm-email";

    public string MailType => MailTypes.EmailConfirmation;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public SecurityLinkPurpose? LinkPurpose => SecurityLinkPurpose.EmailConfirmation;

    public bool DescribesPasswordChange => false;

    public MailContent Compose(MailRecipient recipient)
    {
        var link = recipient.Link ?? throw new InvalidOperationException("A confirmation mail needs its link.");
        var locale = recipient.Locale;
        var title = catalog.Translate(locale, "emailConfirmation.title");
        var target = new Uri(options.Value.SiteUri!, $"{ConfirmationPath}#token={link.Token}");
        var hours = Math.Max(1, (int)Math.Round((link.ExpiresAt - timeProvider.GetUtcNow()).TotalHours));

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(catalog.Phrase(locale, "emailConfirmation.greeting")),
                new MailParagraph(catalog.Phrase(
                    locale,
                    "emailConfirmation.ownership",
                    new MailArgument("email", recipient.Email, IsEmphasized: true))),
                new MailButton(catalog.Translate(locale, "emailConfirmation.confirm"), target, IsPrimary: true),
                new MailParagraph(
                    MailText.Plain(catalog.Translate(
                        locale,
                        "emailConfirmation.expiry",
                        new Dictionary<string, object?> { ["hours"] = hours })),
                    IsAside: true),
                new MailLinkCopy(catalog.Translate(locale, "common.linkCopy"), target),
                new MailParagraph(catalog.Phrase(locale, "emailConfirmation.ifItWasNotYou"), IsAside: true)
            ],
            [
                catalog.Translate(locale, "emailConfirmation.reason"),
                catalog.Translate(locale, "common.tagline")
            ]);
    }
}

using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class PasswordResetMail(
    ITranslationCatalog catalog,
    IOptions<MailOptions> options,
    TimeProvider timeProvider) : IMailTemplate
{
    private const string ResetPath = "account/reset-password";

    public string MailType => MailTypes.PasswordReset;

    public TimeSpan Lifetime { get; } = TimeSpan.FromHours(1);

    public SecurityLinkPurpose? LinkPurpose => SecurityLinkPurpose.PasswordReset;

    public bool DescribesPasswordChange => false;

    public MailContent Compose(MailRecipient recipient)
    {
        var link = recipient.Link ?? throw new InvalidOperationException("A reset mail needs its link.");
        var locale = recipient.Locale;
        var title = catalog.Translate(locale, "passwordReset.title");
        var target = new Uri(options.Value.SiteUri!, $"{ResetPath}#token={link.Token}");
        var hours = Math.Max(1, (int)Math.Round((link.ExpiresAt - timeProvider.GetUtcNow()).TotalHours));
        var greeting = recipient.DisplayName is { } displayName
            ? catalog.Phrase(locale, "common.greeting", new MailArgument("name", displayName))
            : catalog.Phrase(locale, "common.greetingWithoutName");

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(greeting),
                new MailParagraph(catalog.Phrase(locale, "passwordReset.request")),
                new MailButton(catalog.Translate(locale, "passwordReset.choose"), target, IsPrimary: true),
                new MailParagraph(
                    MailText.Plain(catalog.Translate(
                        locale,
                        "passwordReset.expiry",
                        new Dictionary<string, object?> { ["hours"] = hours })),
                    IsAside: true),
                new MailLinkCopy(catalog.Translate(locale, "common.linkCopy"), target),
                new MailParagraph(catalog.Phrase(locale, "passwordReset.ifItWasNotYou"), IsAside: true)
            ],
            [
                catalog.Translate(locale, "passwordReset.reason"),
                catalog.Translate(locale, "common.tagline")
            ]);
    }
}

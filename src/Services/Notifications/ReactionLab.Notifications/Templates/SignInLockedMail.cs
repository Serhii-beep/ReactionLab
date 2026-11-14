using System.Globalization;
using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class SignInLockedMail(ITranslationCatalog catalog, IOptions<MailOptions> options)
    : IMailTemplate, IRepeatLimitedMailTemplate
{
    private const string ForgotPasswordPath = "account/forgot-password";
    private static readonly TimeSpan IntervalBetweenPauseNotices = TimeSpan.FromDays(1);

    public string MailType => MailTypes.SignInLocked;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public SecurityLinkPurpose? LinkPurpose => null;

    public MailDetailKind Detail => MailDetailKind.SignInLockout;

    public TimeSpan? RepeatIntervalFor(MailDetail detail) =>
        detail is CurrentLockout { Disabled: false } ? IntervalBetweenPauseNotices : null;

    public MailContent Compose(MailRecipient recipient)
    {
        var lockout = recipient.Detail as CurrentLockout
            ?? throw new InvalidOperationException("A lockout notice needs the account's lockout.");
        var locale = recipient.Locale;
        var section = lockout.Disabled ? "signInLocked.disabled" : "signInLocked.paused";
        var title = catalog.Translate(locale, $"{section}.title");
        var failures = lockout.Failures.ToString(CultureInfo.GetCultureInfo(locale));
        var failuresFact = new MailFact(catalog.Translate(locale, "common.wrongPasswords"), failures);
        IReadOnlyList<MailFact> facts = lockout is { Disabled: false, PausedUntil: { } pausedUntil }
            ?
            [
                new MailFact(
                    catalog.Translate(locale, "common.pausedUntil"),
                    MailTimes.DescribeMoment(pausedUntil, recipient.TimeZone, locale)),
                failuresFact
            ]
            : [failuresFact];

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(MailGreetings.For(catalog, recipient)),
                new MailParagraph(lockout.Disabled
                    ? catalog.Phrase(locale, $"{section}.happened", new MailArgument("count", failures))
                    : catalog.Phrase(locale, $"{section}.happened")),
                new MailFacts(facts),
                new MailParagraph(catalog.Phrase(locale, $"{section}.ifItWasYou")),
                new MailParagraph(catalog.Phrase(locale, $"{section}.ifItWasNotYou")),
                new MailButton(
                    catalog.Translate(locale, "signInLocked.reset"),
                    new Uri(options.Value.SiteUri!, ForgotPasswordPath))
            ],
            [
                catalog.Translate(locale, "signInLocked.reason"),
                catalog.Translate(locale, "common.tagline")
            ]);
    }
}

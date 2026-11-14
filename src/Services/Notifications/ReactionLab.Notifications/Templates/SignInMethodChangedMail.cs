using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class SignInMethodChangedMail(ITranslationCatalog catalog, IOptions<MailOptions> options)
    : IMailTemplate
{
    private const string SignInMethodsPath = "settings/sign-in-methods";
    private const string ForgotPasswordPath = "account/forgot-password";
    private const string MethodSeparator = " · ";

    public string MailType => MailTypes.SignInMethodChanged;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public SecurityLinkPurpose? LinkPurpose => null;

    public MailDetailKind Detail => MailDetailKind.SignInMethodChange;

    public MailContent Compose(MailRecipient recipient)
    {
        var change = recipient.Detail as RecordedSignInMethodChange
            ?? throw new InvalidOperationException("A sign-in method notice needs its change.");
        var locale = recipient.Locale;
        var section = $"signInMethodChanged.{SectionOf(change.Change)}";
        MailArgument[] provider = change.ProviderDisplayName is { Length: > 0 } providerName
            ? [new MailArgument("provider", providerName)]
            : [];
        var title = catalog.Phrase(locale, $"{section}.title", provider).ToString();
        var resetUndoesIt = change.Change
            is SignInMethodChangeKind.ProviderDisconnected
            or SignInMethodChangeKind.PasswordRemoved;

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(MailGreetings.For(catalog, recipient)),
                new MailParagraph(catalog.Phrase(locale, $"{section}.changed", provider)),
                new MailFacts(
                [
                    new MailFact(
                        catalog.Translate(locale, "common.when"),
                        MailTimes.DescribeMoment(change.ChangedAt, recipient.TimeZone, locale)),
                    new MailFact(
                        catalog.Translate(locale, "common.device"),
                        MailDevices.Describe(catalog, locale, change.Browser, change.OperatingSystem)),
                    new MailFact(
                        catalog.Translate(locale, "signInMethodChanged.methods"),
                        DescribeMethods(change, locale))
                ]),
                new MailParagraph(catalog.Phrase(locale, "signInMethodChanged.ifItWasYou")),
                new MailParagraph(catalog.Phrase(locale, $"{section}.ifItWasNotYou", provider)),
                resetUndoesIt
                    ? new MailButton(
                        catalog.Translate(locale, "signInMethodChanged.reset"),
                        new Uri(options.Value.SiteUri!, ForgotPasswordPath))
                    : new MailButton(
                        catalog.Translate(locale, "signInMethodChanged.review"),
                        new Uri(options.Value.SiteUri!, SignInMethodsPath))
            ],
            [
                catalog.Translate(locale, "signInMethodChanged.reason"),
                catalog.Translate(locale, "common.tagline")
            ]);
    }

    private static string SectionOf(SignInMethodChangeKind change) =>
        change switch
        {
            SignInMethodChangeKind.ProviderConnected => "connected",
            SignInMethodChangeKind.ProviderDisconnected => "disconnected",
            SignInMethodChangeKind.PasswordSet => "passwordSet",
            SignInMethodChangeKind.PasswordRemoved => "passwordRemoved",
            SignInMethodChangeKind.AccountTakenOver => "takenOver",
            _ => throw new InvalidOperationException($"No notice describes the change {change}.")
        };

    private string DescribeMethods(RecordedSignInMethodChange change, string locale)
    {
        var methods = new List<string>();

        if (change.HasPassword)
        {
            methods.Add(catalog.Translate(locale, "signInMethodChanged.password"));
        }

        methods.AddRange(change.ProviderDisplayNames);

        return methods.Count == 0
            ? catalog.Translate(locale, "signInMethodChanged.noMethods")
            : string.Join(MethodSeparator, methods);
    }
}

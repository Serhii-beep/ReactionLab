using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class PlatformAdminChangedMail(ITranslationCatalog catalog, IOptions<MailOptions> options)
    : IMailTemplate
{
    private const string PlatformRolesPath = "admin/platform-roles";
    private const string NameSeparator = " · ";

    public string MailType => MailTypes.PlatformAdminChanged;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public SecurityLinkPurpose? LinkPurpose => null;

    public MailDetailKind Detail => MailDetailKind.PlatformRoleChange;

    public MailContent Compose(MailRecipient recipient)
    {
        var change = recipient.Detail as RecordedPlatformRoleChange
            ?? throw new InvalidOperationException("A platform admin notice needs its change.");
        var locale = recipient.Locale;
        var section = change.Granted ? "platformAdminChanged.granted" : "platformAdminChanged.revoked";
        var title = catalog.Translate(locale, $"{section}.title");
        var account = new MailArgument("account", change.AccountDisplayName, IsEmphasized: true);

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(MailGreetings.For(catalog, recipient)),
                new MailParagraph(catalog.Phrase(locale, $"{section}.changed", account)),
                new MailFacts(
                [
                    new MailFact(
                        catalog.Translate(locale, "platformAdminChanged.account"),
                        Describe(change.AccountDisplayName, change.AccountEmail)),
                    new MailFact(
                        catalog.Translate(locale, "platformAdminChanged.changedBy"),
                        DescribeAuthor(change, locale)),
                    new MailFact(
                        catalog.Translate(locale, "common.when"),
                        MailTimes.DescribeMoment(change.ChangedAt, recipient.TimeZone, locale))
                ]),
                new MailParagraph(catalog.Phrase(locale, "platformAdminChanged.everyAdmin")),
                new MailParagraph(catalog.Phrase(locale, "platformAdminChanged.ifUnexpected")),
                new MailButton(
                    catalog.Translate(locale, "platformAdminChanged.review"),
                    new Uri(options.Value.SiteUri!, PlatformRolesPath))
            ],
            [
                catalog.Translate(locale, "platformAdminChanged.reason"),
                catalog.Translate(locale, "common.tagline")
            ]);
    }

    private static string Describe(string displayName, string email) =>
        email.Length == 0 ? displayName : $"{displayName}{NameSeparator}{email}";

    private string DescribeAuthor(RecordedPlatformRoleChange change, string locale) =>
        change.Source switch
        {
            PlatformRoleChangeSource.FirstAdmin => catalog.Translate(locale, "platformAdminChanged.firstAdminSetting"),
            PlatformRoleChangeSource.BreakGlass => catalog.Translate(locale, "platformAdminChanged.breakGlassSetting"),
            _ => Describe(change.ChangedByDisplayName, change.ChangedByEmail)
        };
}

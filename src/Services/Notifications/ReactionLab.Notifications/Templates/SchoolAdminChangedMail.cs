using System.Globalization;
using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class SchoolAdminChangedMail(ITranslationCatalog catalog, IOptions<MailOptions> options)
    : IMailTemplate
{
    private const string MembersPath = "school/members";
    private const string DistrictAdminRole = "district-admin";
    private const string NameSeparator = " · ";

    public string MailType => MailTypes.SchoolAdminChanged;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public SecurityLinkPurpose? LinkPurpose => null;

    public MailDetailKind Detail => MailDetailKind.SchoolAdminChange;

    public MailContent Compose(MailRecipient recipient)
    {
        var change = recipient.Detail as RecordedSchoolAdminChange
            ?? throw new InvalidOperationException("A school admin notice needs its change.");
        var locale = recipient.Locale;
        var imported = change.AccountCount > 1;
        var section = imported ? "schoolAdminChanged.imported" : $"schoolAdminChanged.{SectionOf(change.Change)}";
        var organization = new MailArgument("organization", change.OrganizationName);
        var count = new MailArgument("count", change.AccountCount.ToString(CultureInfo.GetCultureInfo(locale)));
        var districtAdmin = change.Role == DistrictAdminRole;
        var role = new MailArgument(
            "role",
            catalog.Translate(
                locale,
                districtAdmin ? "schoolAdminChanged.districtAdmin" : "schoolAdminChanged.schoolAdmin"));
        var account = new MailArgument("account", change.AccountDisplayName, IsEmphasized: true);
        var title = (imported
                ? catalog.Phrase(locale, $"{section}.title", count, organization)
                : catalog.Phrase(locale, $"{section}.title", organization))
            .ToString();
        var deleted = change.Change == AdminRoleChange.AccountDeleted;
        var changed = (imported, deleted) switch
        {
            (true, _) => catalog.Phrase(
                locale,
                $"{section}.changed",
                count,
                new MailArgument(
                    "roles",
                    catalog.Translate(
                        locale,
                        districtAdmin ? "schoolAdminChanged.districtAdmins" : "schoolAdminChanged.schoolAdmins")),
                organization,
                account),
            (_, true) => catalog.Phrase(locale, $"{section}.changed", role, organization),
            _ => catalog.Phrase(locale, $"{section}.changed", account, role, organization)
        };
        var facts = new List<MailFact>();

        if (!deleted)
        {
            facts.Add(new MailFact(
                catalog.Translate(locale, "schoolAdminChanged.account"),
                change.AccountUsername.Length == 0
                    ? change.AccountDisplayName
                    : $"{change.AccountDisplayName}{NameSeparator}{change.AccountUsername}"));
        }

        facts.Add(new MailFact(
            catalog.Translate(locale, "schoolAdminChanged.changedBy"),
            change.ChangedByPlatform
                ? catalog.Translate(locale, "schoolAdminChanged.changedByPlatform")
                : change.ChangedByDisplayName));
        facts.Add(new MailFact(
            catalog.Translate(locale, "common.when"),
            MailTimes.DescribeMoment(change.ChangedAt, recipient.TimeZone, locale)));

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(MailGreetings.For(catalog, recipient)),
                new MailParagraph(changed),
                new MailFacts(facts),
                new MailParagraph(catalog.Phrase(locale, "schoolAdminChanged.everyAdmin", organization)),
                new MailParagraph(catalog.Phrase(locale, "schoolAdminChanged.ifUnexpected")),
                new MailButton(
                    catalog.Translate(locale, "schoolAdminChanged.review"),
                    new Uri(options.Value.SiteUri!, MembersPath))
            ],
            [
                catalog.Phrase(locale, "schoolAdminChanged.reason", organization).ToString(),
                catalog.Translate(locale, "common.tagline")
            ],
            change.OrganizationName);
    }

    private static string SectionOf(AdminRoleChange change) =>
        change switch
        {
            AdminRoleChange.Granted => "granted",
            AdminRoleChange.Revoked => "revoked",
            AdminRoleChange.AccountDeleted => "deleted",
            _ => throw new InvalidOperationException($"No notice describes the change {change}.")
        };
}

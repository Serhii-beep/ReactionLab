using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class SchoolInvitationMail(ITranslationCatalog catalog, IOptions<MailOptions> options) : IMailTemplate
{
    private const string SetupPath = "account/setup";

    public string MailType => MailTypes.SchoolInvitation;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public SecurityLinkPurpose? LinkPurpose => SecurityLinkPurpose.AccountSetup;

    public MailDetailKind Detail => MailDetailKind.AccountSetup;

    public MailContent Compose(MailRecipient recipient)
    {
        var setup = recipient.Detail as MailedAccountSetup
            ?? throw new InvalidOperationException("An invitation needs its setup.");
        var link = recipient.Link ?? throw new InvalidOperationException("An invitation needs its link.");
        var locale = recipient.Locale;
        var target = new Uri(options.Value.SiteUri!, $"{SetupPath}#token={link.Token}");
        var organization = new MailArgument("organization", setup.OrganizationName, IsEmphasized: true);
        var title = catalog.Phrase(locale, "schoolInvitation.title", organization with { IsEmphasized = false })
            .ToString();
        var role = new MailArgument("role", SchoolRoles.Describe(catalog, locale, setup.Roles), IsEmphasized: true);
        var invited = (setup.RequestedByPlatformAdmin, setup.RequestedByDisplayName.Length == 0) switch
        {
            (true, _) => catalog.Phrase(locale, "schoolInvitation.firstAdmin", organization),
            (_, true) => catalog.Phrase(locale, "schoolInvitation.invitedBySchool", organization, role),
            _ => catalog.Phrase(
                locale,
                "schoolInvitation.invited",
                new MailArgument("inviter", setup.RequestedByDisplayName),
                organization,
                role)
        };
        var accountReady = setup.Username.Length == 0
            ? catalog.Phrase(locale, "schoolInvitation.chooseUsername")
            : catalog.Phrase(
                locale,
                "schoolInvitation.choosePassword",
                new MailArgument("username", setup.Username, IsEmphasized: true));

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(MailGreetings.For(catalog, recipient)),
                new MailParagraph(invited),
                new MailParagraph(accountReady),
                new MailButton(catalog.Translate(locale, "schoolInvitation.setUp"), target, IsPrimary: true),
                new MailParagraph(
                    catalog.Phrase(
                        locale,
                        "schoolInvitation.expiry",
                        new MailArgument("date", MailTimes.DescribeDate(link.ExpiresAt, recipient.TimeZone, locale))),
                    IsAside: true),
                new MailLinkCopy(catalog.Translate(locale, "common.linkCopy"), target),
                new MailParagraph(catalog.Phrase(locale, "schoolInvitation.ifUnexpected"), IsAside: true)
            ],
            [
                catalog.Phrase(
                    locale,
                    setup.RequestedByPlatformAdmin ? "schoolInvitation.firstAdminReason" : "schoolInvitation.reason",
                    organization with { IsEmphasized = false }).ToString(),
                catalog.Translate(locale, "common.tagline")
            ],
            setup.OrganizationName);
    }
}

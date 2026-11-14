using Microsoft.Extensions.Options;
using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Sending;

namespace ReactionLab.Notifications.Templates;

internal sealed class SchoolPasswordResetMail(
    ITranslationCatalog catalog,
    IOptions<MailOptions> options,
    TimeProvider timeProvider) : IMailTemplate
{
    private const string SetupPath = "account/setup";
    private static readonly TimeSpan ShortestRemainingInHours = TimeSpan.FromMinutes(55);

    public string MailType => MailTypes.SchoolPasswordReset;

    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    public SecurityLinkPurpose? LinkPurpose => SecurityLinkPurpose.AccountSetup;

    public MailDetailKind Detail => MailDetailKind.AccountSetup;

    public MailContent Compose(MailRecipient recipient)
    {
        var setup = recipient.Detail as MailedAccountSetup
            ?? throw new InvalidOperationException("A school reset mail needs its setup.");
        var link = recipient.Link ?? throw new InvalidOperationException("A school reset mail needs its link.");
        var locale = recipient.Locale;
        var target = new Uri(options.Value.SiteUri!, $"{SetupPath}#token={link.Token}");
        var organization = new MailArgument("organization", setup.OrganizationName, IsEmphasized: true);
        var requestedByAccount = setup.Purpose == AccountSetupPurpose.SelfReset;
        var title = catalog.Translate(
            locale,
            requestedByAccount ? "schoolPasswordReset.requestedTitle" : "schoolPasswordReset.resetTitle");
        var happened = requestedByAccount
            ? catalog.Phrase(locale, "schoolPasswordReset.requested", organization)
            : setup.RequestedByPlatformAdmin
                ? catalog.Phrase(locale, "schoolPasswordReset.resetByPlatform", organization)
                : setup.RequestedByDisplayName.Length == 0
                    ? catalog.Phrase(locale, "schoolPasswordReset.resetBySchool", organization)
                    : catalog.Phrase(
                        locale,
                        "schoolPasswordReset.resetByMember",
                        new MailArgument("requester", setup.RequestedByDisplayName),
                        new MailArgument("role", SchoolRoles.Describe(catalog, locale, setup.RequestedByRoles)),
                        organization);
        var remaining = link.ExpiresAt - timeProvider.GetUtcNow();
        var expiry = requestedByAccount
            ? MailText.Plain(remaining >= ShortestRemainingInHours
                ? catalog.Translate(
                    locale,
                    "schoolPasswordReset.expiresInHours",
                    new Dictionary<string, object?> { ["hours"] = Math.Max(1, (int)Math.Round(remaining.TotalHours)) })
                : catalog.Translate(
                    locale,
                    "schoolPasswordReset.expiresInMinutes",
                    new Dictionary<string, object?> { ["minutes"] = Math.Max(1, (int)remaining.TotalMinutes) }))
            : catalog.Phrase(
                locale,
                "schoolPasswordReset.expiresOnDate",
                new MailArgument("date", MailTimes.DescribeDate(link.ExpiresAt, recipient.TimeZone, locale)));
        var facts = new List<MailFact>
        {
            new(catalog.Translate(locale, "schoolPasswordReset.signInCode"), setup.SignInCode)
        };

        if (setup.Username.Length > 0)
        {
            facts.Add(new MailFact(catalog.Translate(locale, "schoolPasswordReset.username"), setup.Username));
        }

        return new MailContent(
            locale,
            title,
            title,
            [
                new MailParagraph(MailGreetings.For(catalog, recipient)),
                new MailParagraph(happened),
                new MailFacts(facts),
                new MailButton(catalog.Translate(locale, "schoolPasswordReset.choose"), target, IsPrimary: true),
                new MailParagraph(expiry, IsAside: true),
                new MailLinkCopy(catalog.Translate(locale, "common.linkCopy"), target),
                new MailParagraph(
                    catalog.Phrase(
                        locale,
                        requestedByAccount ? "schoolPasswordReset.ifItWasNotYou" : "schoolPasswordReset.questions"),
                    IsAside: true)
            ],
            [
                catalog.Phrase(
                    locale,
                    "schoolPasswordReset.reason",
                    organization with { IsEmphasized = false }).ToString(),
                catalog.Translate(locale, "common.tagline")
            ],
            setup.OrganizationName);
    }
}

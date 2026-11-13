using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;

namespace ReactionLab.Notifications.Templates;

internal interface IMailTemplate
{
    string MailType { get; }

    TimeSpan Lifetime { get; }

    SecurityLinkPurpose? LinkPurpose { get; }

    MailContent Compose(MailRecipient recipient);
}

using ReactionLab.Notifications.Mail;

namespace ReactionLab.Notifications.Templates;

internal interface IMailTemplate
{
    string MailType { get; }

    TimeSpan Lifetime { get; }

    MailContent Compose(MailRecipient recipient);
}

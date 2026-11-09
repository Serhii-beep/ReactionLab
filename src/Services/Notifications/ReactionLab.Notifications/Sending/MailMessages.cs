using MimeKit;
using ReactionLab.Notifications.Mail;

namespace ReactionLab.Notifications.Sending;

internal static class MailMessages
{
    public static MimeMessage Create(Guid jobId, MailboxAddress sender, MailboxAddress recipient, RenderedMail rendered)
    {
        var body = new BodyBuilder { HtmlBody = rendered.Html, TextBody = rendered.Text };
        var logo = body.LinkedResources.Add(
            MailImages.LogoFileName,
            MailImages.OpenLogo(),
            new ContentType("image", "png"));

        logo.ContentId = MailImages.LogoContentId;

        var message = new MimeMessage
        {
            Subject = rendered.Subject,
            MessageId = $"{jobId}@{sender.Domain}",
            Body = body.ToMessageBody()
        };

        message.From.Add(sender);
        message.To.Add(recipient);
        message.Headers.Add("Auto-Submitted", "auto-generated");

        return message;
    }
}

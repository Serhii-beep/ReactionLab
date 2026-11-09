using System.Diagnostics;
using ReactionLab.Notifications.Persistence.Mail;

namespace ReactionLab.Notifications.Sending;

internal static class MailTelemetry
{
    public const string SourceName = "ReactionLab.Notifications.Mail";
    public const string MailTypeTag = "reactionlab.mail.type";
    public const string OutcomeTag = "reactionlab.mail.outcome";

    private static readonly ActivitySource Source = new(SourceName);

    public static Activity? StartSend(MailJob job)
    {
        ActivityContext.TryParse(job.TraceParent, null, isRemote: true, out var parent);

        var activity = Source.StartActivity("mail send", ActivityKind.Internal, parent);
        activity?.SetTag(MailTypeTag, job.MailType);
        activity?.SetTag("reactionlab.mail.job_id", job.Id.ToString());
        activity?.SetTag("reactionlab.mail.attempt", job.Attempts);

        return activity;
    }
}

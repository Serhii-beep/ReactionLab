namespace ReactionLab.Notifications.Sending;

internal static class MailJobOutcomes
{
    public const string Sent = "sent";
    public const string Expired = "expired";
    public const string RecipientGone = "recipient_gone";
    public const string NoAddress = "no_address";
    public const string UnsendableAddress = "unsendable_address";
    public const string LinkUnavailable = "link_unavailable";
    public const string ChangeUnavailable = "change_unavailable";
    public const string Retried = "retried";
    public const string Deferred = "deferred";
    public const string WaitingForNewerIdentity = "waiting_for_newer_identity";
    public const string Failed = "failed";
}

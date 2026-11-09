namespace ReactionLab.Notifications.Sending;

internal readonly record struct MailBatchOutcome(int Claimed, bool Paused);

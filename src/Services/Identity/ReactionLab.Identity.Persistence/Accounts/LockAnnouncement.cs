using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record LockAnnouncement(OutboxMessage Locked, OutboxMessage Disabled);

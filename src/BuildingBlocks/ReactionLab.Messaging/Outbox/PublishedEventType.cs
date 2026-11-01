using ReactionLab.SharedKernel;

namespace ReactionLab.Messaging.Outbox;

internal sealed record PublishedEventType(
    string Topic,
    string CloudEventType,
    Func<IDomainEvent, string> SubjectOf,
    Func<IDomainEvent, string> ToJson);

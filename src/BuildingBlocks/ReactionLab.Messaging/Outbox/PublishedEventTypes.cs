using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ReactionLab.SharedKernel;

namespace ReactionLab.Messaging.Outbox;

public sealed class PublishedEventTypes
{
    public const int MaximumPayloadBytes = 64 * 1024;

    private readonly Dictionary<Type, PublishedEventType> _typesByEventType = [];
    private readonly List<string> _topics = [];
    private readonly List<string> _stateTopics = [];

    public IReadOnlyList<string> Topics => _topics;

    public IReadOnlyList<string> StateTopics => _stateTopics;

    public PublishedEventTypes Add<TEvent>(
        string topic,
        string cloudEventType,
        Func<TEvent, Guid> subjectOf,
        JsonTypeInfo<TEvent> jsonTypeInfo)
        where TEvent : IDomainEvent =>
        Register(topic, cloudEventType, subjectOf, jsonTypeInfo, isStateTopic: false);

    public PublishedEventTypes AddState<TEvent>(
        string topic,
        string cloudEventType,
        Func<TEvent, Guid> subjectOf,
        JsonTypeInfo<TEvent> jsonTypeInfo)
        where TEvent : IDomainEvent =>
        Register(topic, cloudEventType, subjectOf, jsonTypeInfo, isStateTopic: true);

    public OutboxMessage ToOutboxMessage(IDomainEvent domainEvent, string? traceParent, DateTimeOffset createdAt)
    {
        if (!_typesByEventType.TryGetValue(domainEvent.GetType(), out var eventType))
        {
            throw new InvalidOperationException($"{domainEvent.GetType().Name} is not a published event type.");
        }

        var payload = eventType.ToJson(domainEvent);

        if (Encoding.UTF8.GetByteCount(payload) > MaximumPayloadBytes)
        {
            throw new InvalidOperationException($"{domainEvent.GetType().Name} is larger than an event may be.");
        }

        return OutboxMessage.ForEvent(
            domainEvent.EventId,
            eventType.Topic,
            eventType.SubjectOf(domainEvent),
            eventType.CloudEventType,
            payload,
            traceParent,
            createdAt);
    }

    private PublishedEventTypes Register<TEvent>(
        string topic,
        string cloudEventType,
        Func<TEvent, Guid> subjectOf,
        JsonTypeInfo<TEvent> jsonTypeInfo,
        bool isStateTopic)
        where TEvent : IDomainEvent
    {
        if (_topics.Contains(topic) && _stateTopics.Contains(topic) != isStateTopic)
        {
            throw new InvalidOperationException($"{topic} can't be both an event topic and a state topic.");
        }

        _typesByEventType.Add(typeof(TEvent), new PublishedEventType(
            topic,
            cloudEventType,
            domainEvent => subjectOf((TEvent)domainEvent).ToString(),
            domainEvent => JsonSerializer.Serialize((TEvent)domainEvent, jsonTypeInfo)));

        if (!_topics.Contains(topic))
        {
            _topics.Add(topic);
        }

        if (isStateTopic && !_stateTopics.Contains(topic))
        {
            _stateTopics.Add(topic);
        }

        return this;
    }
}

using System.Text.Json.Serialization;

namespace ReactionLab.SharedKernel;

public abstract record DomainEvent : IDomainEvent
{
    [JsonIgnore]
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

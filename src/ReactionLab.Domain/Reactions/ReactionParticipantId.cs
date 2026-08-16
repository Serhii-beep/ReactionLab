using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reactions;

public readonly record struct ReactionParticipantId(Guid Value) : IStronglyTypedId<ReactionParticipantId>
{
    public static ReactionParticipantId From(Guid value) => new(value);

    public static ReactionParticipantId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

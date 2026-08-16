using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reactions;

public readonly record struct ReactionId(Guid Value) : IStronglyTypedId<ReactionId>
{
    public static ReactionId From(Guid value) => new(value);

    public static ReactionId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

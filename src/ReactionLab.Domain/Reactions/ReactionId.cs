using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reactions;

public readonly record struct ReactionId(Guid Value) : IStronglyTypedId<ReactionId>, IComparable<ReactionId>
{
    public static ReactionId From(Guid value) => new(value);

    public static ReactionId New() => new(Guid.CreateVersion7());

    public int CompareTo(ReactionId other) => Value.CompareTo(other.Value);

    public static bool operator <(ReactionId left, ReactionId right) => left.CompareTo(right) < 0;

    public static bool operator >(ReactionId left, ReactionId right) => left.CompareTo(right) > 0;

    public static bool operator <=(ReactionId left, ReactionId right) => left.CompareTo(right) <= 0;

    public static bool operator >=(ReactionId left, ReactionId right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value.ToString();
}

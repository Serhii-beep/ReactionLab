using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Substances;

public readonly record struct SubstanceId(Guid Value) : IStronglyTypedId<SubstanceId>, IComparable<SubstanceId>
{
    public static SubstanceId From(Guid value) => new(value);

    public static SubstanceId New() => new(Guid.CreateVersion7());

    public int CompareTo(SubstanceId other) => Value.CompareTo(other.Value);

    public static bool operator <(SubstanceId left, SubstanceId right) => left.CompareTo(right) < 0;

    public static bool operator >(SubstanceId left, SubstanceId right) => left.CompareTo(right) > 0;

    public static bool operator <=(SubstanceId left, SubstanceId right) => left.CompareTo(right) <= 0;

    public static bool operator >=(SubstanceId left, SubstanceId right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value.ToString();
}

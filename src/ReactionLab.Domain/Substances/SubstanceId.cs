using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Substances;

public readonly record struct SubstanceId(Guid Value) : IStronglyTypedId<SubstanceId>
{
    public static SubstanceId From(Guid value) => new(value);

    public static SubstanceId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

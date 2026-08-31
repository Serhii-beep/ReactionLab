using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reference;

public readonly record struct ChemistryReferenceId(Guid Value) : IStronglyTypedId<ChemistryReferenceId>
{
    public static ChemistryReferenceId From(Guid value) => new(value);

    public static ChemistryReferenceId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

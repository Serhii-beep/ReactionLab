using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Domain.Elements;

public readonly record struct ElementId(Guid Value) : IStronglyTypedId<ElementId>
{
    public static ElementId From(Guid value) => new(value);

    public static ElementId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

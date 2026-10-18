using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Domain.Releases;

public readonly record struct CatalogReleaseId(Guid Value) : IStronglyTypedId<CatalogReleaseId>
{
    public static CatalogReleaseId From(Guid value) => new(value);

    public static CatalogReleaseId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

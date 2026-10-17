using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal static class StronglyTypedIdFactory
{
    public static TId From<TId>(Guid value)
        where TId : struct, IStronglyTypedId<TId> => TId.From(value);
}

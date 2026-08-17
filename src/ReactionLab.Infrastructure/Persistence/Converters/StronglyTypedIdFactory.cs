using ReactionLab.Domain.Common;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal static class StronglyTypedIdFactory
{
    public static TId From<TId>(Guid value)
        where TId : struct, IStronglyTypedId<TId> => TId.From(value);
}

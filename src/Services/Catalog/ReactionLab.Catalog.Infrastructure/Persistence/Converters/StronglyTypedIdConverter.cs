using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class StronglyTypedIdConverter<TId>()
    : ValueConverter<TId, Guid>(id => id.Value, value => StronglyTypedIdFactory.From<TId>(value))
    where TId : struct, IStronglyTypedId<TId>;

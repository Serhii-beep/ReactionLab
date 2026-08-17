using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Common;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class StronglyTypedIdConverter<TId>()
    : ValueConverter<TId, Guid>(id => id.Value, value => StronglyTypedIdFactory.From<TId>(value))
    where TId : struct, IStronglyTypedId<TId>;

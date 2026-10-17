using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Reference;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class ReferenceKeyConverter()
    : ValueConverter<ReferenceKey, string>(key => key.Value, value => ReferenceKey.Create(value).Value);

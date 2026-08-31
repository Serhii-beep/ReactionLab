using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Reference;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class ReferenceKeyConverter()
    : ValueConverter<ReferenceKey, string>(key => key.Value, value => ReferenceKey.Create(value).Value);

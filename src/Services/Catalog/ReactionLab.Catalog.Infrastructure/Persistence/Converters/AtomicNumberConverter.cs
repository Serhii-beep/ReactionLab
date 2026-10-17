using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Elements;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class AtomicNumberConverter()
    : ValueConverter<AtomicNumber, int>(number => number.Value, value => AtomicNumber.Create(value).Value);

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class AtomicNumberConverter()
    : ValueConverter<AtomicNumber, int>(number => number.Value, value => AtomicNumber.Create(value).Value);

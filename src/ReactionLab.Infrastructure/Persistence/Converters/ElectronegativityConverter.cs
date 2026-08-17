using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class ElectronegativityConverter()
    : ValueConverter<Electronegativity, decimal>(
        electronegativity => electronegativity.Pauling, value => Electronegativity.Create(value).Value);

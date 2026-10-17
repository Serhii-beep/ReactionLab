using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Elements;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class ElectronegativityConverter()
    : ValueConverter<Electronegativity, decimal>(
        electronegativity => electronegativity.Pauling, value => Electronegativity.Create(value).Value);

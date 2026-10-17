using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Common;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class PressureConverter()
    : ValueConverter<Pressure, decimal>(
        pressure => pressure.Kilopascals, value => Pressure.FromKilopascals(value).Value);

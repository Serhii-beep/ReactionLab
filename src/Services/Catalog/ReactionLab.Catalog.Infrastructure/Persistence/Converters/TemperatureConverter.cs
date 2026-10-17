using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Common;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class TemperatureConverter()
    : ValueConverter<Temperature, decimal>(
        temperature => temperature.Kelvin, value => Temperature.FromKelvin(value).Value);

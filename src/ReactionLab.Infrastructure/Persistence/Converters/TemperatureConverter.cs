using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.SharedKernel;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class TemperatureConverter()
    : ValueConverter<Temperature, decimal>(
        temperature => temperature.Kelvin, value => Temperature.FromKelvin(value).Value);

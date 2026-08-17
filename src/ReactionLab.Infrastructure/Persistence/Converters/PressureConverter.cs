using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.SharedKernel;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class PressureConverter()
    : ValueConverter<Pressure, decimal>(
        pressure => pressure.Kilopascals, value => Pressure.FromKilopascals(value).Value);

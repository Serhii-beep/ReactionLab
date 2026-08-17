using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.SharedKernel;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class EnthalpyConverter()
    : ValueConverter<Enthalpy, decimal>(
        enthalpy => enthalpy.KilojoulesPerMole, value => Enthalpy.FromKilojoulesPerMole(value).Value);

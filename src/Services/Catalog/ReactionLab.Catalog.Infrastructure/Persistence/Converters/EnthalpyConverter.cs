using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Common;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class EnthalpyConverter()
    : ValueConverter<Enthalpy, decimal>(
        enthalpy => enthalpy.KilojoulesPerMole, value => Enthalpy.FromKilojoulesPerMole(value).Value);

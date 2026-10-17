using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class ChemicalFormulaConverter()
    : ValueConverter<ChemicalFormula, string>(
        formula => formula.Value, value => ChemicalFormula.Create(value).Value);

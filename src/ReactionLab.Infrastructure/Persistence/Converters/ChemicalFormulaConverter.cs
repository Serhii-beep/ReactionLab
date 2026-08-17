using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class ChemicalFormulaConverter()
    : ValueConverter<ChemicalFormula, string>(
        formula => formula.Value, value => ChemicalFormula.Create(value).Value);

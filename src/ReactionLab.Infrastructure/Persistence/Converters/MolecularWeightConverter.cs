using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class MolecularWeightConverter()
    : ValueConverter<MolecularWeight, decimal>(
        weight => weight.GramsPerMole, value => MolecularWeight.Create(value).Value);

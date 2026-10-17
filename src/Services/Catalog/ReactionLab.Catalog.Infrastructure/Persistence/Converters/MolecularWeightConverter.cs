using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class MolecularWeightConverter()
    : ValueConverter<MolecularWeight, decimal>(
        weight => weight.GramsPerMole, value => MolecularWeight.Create(value).Value);

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Substances;
using ReactionLab.Infrastructure.Persistence.Documents;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class MolecularStructureConverter()
    : ValueConverter<MolecularStructure, string>(
        structure => MolecularStructureDocument.Serialize(structure),
        json => MolecularStructureDocument.Deserialize(json));

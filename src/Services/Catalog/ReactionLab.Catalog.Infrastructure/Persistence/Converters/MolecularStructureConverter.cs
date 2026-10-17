using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.Catalog.Infrastructure.Persistence.Documents;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class MolecularStructureConverter()
    : ValueConverter<MolecularStructure, string>(
        structure => MolecularStructureDocument.Serialize(structure),
        json => MolecularStructureDocument.Deserialize(json));

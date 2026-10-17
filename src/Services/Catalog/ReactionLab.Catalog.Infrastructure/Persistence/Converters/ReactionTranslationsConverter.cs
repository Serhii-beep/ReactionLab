using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Infrastructure.Persistence.Documents;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class ReactionTranslationsConverter()
    : ValueConverter<Translations<ReactionContent>, string>(
        translations => ReactionContentDocument.Serialize(translations),
        json => ReactionContentDocument.Deserialize(json));

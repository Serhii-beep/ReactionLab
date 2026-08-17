using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Reactions;
using ReactionLab.Infrastructure.Persistence.Documents;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class ReactionTranslationsConverter()
    : ValueConverter<Translations<ReactionContent>, string>(
        translations => ReactionContentDocument.Serialize(translations),
        json => ReactionContentDocument.Deserialize(json));

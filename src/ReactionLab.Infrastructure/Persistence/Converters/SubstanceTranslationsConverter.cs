using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Substances;
using ReactionLab.Infrastructure.Persistence.Documents;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class SubstanceTranslationsConverter()
    : ValueConverter<Translations<SubstanceContent>, string>(
        translations => SubstanceContentDocument.Serialize(translations),
        json => SubstanceContentDocument.Deserialize(json));

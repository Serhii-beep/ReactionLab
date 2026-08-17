using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Localization;
using ReactionLab.Infrastructure.Persistence.Documents;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class ElementTranslationsConverter()
    : ValueConverter<Translations<ElementContent>, string>(
        translations => ElementContentDocument.Serialize(translations),
        json => ElementContentDocument.Deserialize(json));

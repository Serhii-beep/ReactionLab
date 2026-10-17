using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Infrastructure.Persistence.Documents;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class ElementTranslationsConverter()
    : ValueConverter<Translations<ElementContent>, string>(
        translations => ElementContentDocument.Serialize(translations),
        json => ElementContentDocument.Deserialize(json));

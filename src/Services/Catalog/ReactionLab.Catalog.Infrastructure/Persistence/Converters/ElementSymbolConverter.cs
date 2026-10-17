using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Elements;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class ElementSymbolConverter()
    : ValueConverter<ElementSymbol, string>(symbol => symbol.Value, value => ElementSymbol.Create(value).Value);

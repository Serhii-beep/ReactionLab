using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class ElementSymbolConverter()
    : ValueConverter<ElementSymbol, string>(symbol => symbol.Value, value => ElementSymbol.Create(value).Value);

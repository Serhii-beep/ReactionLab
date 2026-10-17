using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Common;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class HexColorConverter()
    : ValueConverter<HexColor, string>(color => color.Value, value => HexColor.Create(value).Value);

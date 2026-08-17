using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.SharedKernel;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class HexColorConverter()
    : ValueConverter<HexColor, string>(color => color.Value, value => HexColor.Create(value).Value);

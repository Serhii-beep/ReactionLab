using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Reactions;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class DifficultyLevelConverter()
    : ValueConverter<DifficultyLevel, int>(
        difficulty => difficulty.Value, value => DifficultyLevel.Create(value).Value);

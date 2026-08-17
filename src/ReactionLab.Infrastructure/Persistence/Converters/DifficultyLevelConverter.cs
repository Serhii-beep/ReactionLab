using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Reactions;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class DifficultyLevelConverter()
    : ValueConverter<DifficultyLevel, int>(
        difficulty => difficulty.Value, value => DifficultyLevel.Create(value).Value);

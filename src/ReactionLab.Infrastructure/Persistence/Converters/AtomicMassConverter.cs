using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Infrastructure.Persistence.Converters;

internal sealed class AtomicMassConverter()
    : ValueConverter<AtomicMass, decimal>(mass => mass.Daltons, value => AtomicMass.Create(value).Value);

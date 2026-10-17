using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReactionLab.Catalog.Domain.Elements;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Converters;

internal sealed class AtomicMassConverter()
    : ValueConverter<AtomicMass, decimal>(mass => mass.Daltons, value => AtomicMass.Create(value).Value);

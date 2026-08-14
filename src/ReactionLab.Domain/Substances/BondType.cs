using System.Diagnostics.CodeAnalysis;

namespace ReactionLab.Domain.Substances;

[SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification =
        "Single, Double and Triple are the standard chemical terms for bond order.")]
public enum BondType
{
    Single = 0,
    Double = 1,
    Triple = 2,
    Aromatic = 3,
    Ionic = 4,
    Hydrogen = 5,
    Metallic = 6
}

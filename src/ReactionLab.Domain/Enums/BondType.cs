using System.Diagnostics.CodeAnalysis;

namespace ReactionLab.Domain.Enums;

[SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification =
        "Single, Double, and Triple are the standard chemical terms for bond order.")]
public enum BondType
{
    Single,
    Double,
    Triple,
    Ionic,
    Hydrogen,
    Metallic,
    Covalent
}

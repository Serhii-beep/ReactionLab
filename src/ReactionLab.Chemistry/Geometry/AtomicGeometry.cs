namespace ReactionLab.Chemistry.Geometry;

public readonly record struct AtomicGeometry(
    int ValenceElectrons,
    int SingleBondRadiusPicometres,
    int? DoubleBondRadiusPicometres = null,
    int? TripleBondRadiusPicometres = null)
{
    public int RadiusFor(int order) => order switch
    {
        >= 3 => TripleBondRadiusPicometres ?? DoubleBondRadiusPicometres ?? SingleBondRadiusPicometres,
        2 => DoubleBondRadiusPicometres ?? SingleBondRadiusPicometres,
        _ => SingleBondRadiusPicometres
    };
}

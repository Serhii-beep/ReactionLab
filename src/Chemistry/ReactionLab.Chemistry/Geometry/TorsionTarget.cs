namespace ReactionLab.Chemistry.Geometry;

public readonly record struct TorsionTarget(
    int First,
    int Second,
    int Third,
    int Fourth,
    int Periodicity,
    float PreferredRadians,
    float Weight);

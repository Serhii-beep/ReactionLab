namespace ReactionLab.Chemistry.Geometry;

public readonly record struct DistanceTarget(
    int From,
    int To,
    float Distance,
    float Weight,
    bool FloorOnly);

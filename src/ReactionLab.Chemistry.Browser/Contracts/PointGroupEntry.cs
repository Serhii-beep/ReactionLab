namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record PointGroupEntry(
    string Schoenflies,
    IReadOnlyList<float> CenterAngstroms,
    IReadOnlyList<RotationAxisEntry> RotationAxes,
    IReadOnlyList<MirrorPlaneEntry> MirrorPlanes,
    bool HasInversionCenter);

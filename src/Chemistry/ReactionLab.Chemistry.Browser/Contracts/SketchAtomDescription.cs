using ReactionLab.Chemistry.Valence;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record SketchAtomDescription(
    int FormalCharge,
    bool FormalChargeInferred,
    int LonePairs,
    int OpenValence,
    ValenceStatus Status);

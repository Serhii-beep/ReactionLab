namespace ReactionLab.Chemistry.Valence;

public readonly record struct AtomValence(
    int FormalCharge,
    bool FormalChargeInferred,
    int LonePairs,
    int OpenValence,
    ValenceStatus Status);

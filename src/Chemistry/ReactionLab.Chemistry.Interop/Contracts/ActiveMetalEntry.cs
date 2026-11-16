using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record ActiveMetalEntry(string Symbol, int Charge, WaterReactivity Water);

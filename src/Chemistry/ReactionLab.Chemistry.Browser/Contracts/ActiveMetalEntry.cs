using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ActiveMetalEntry(string Symbol, int Charge, WaterReactivity Water);

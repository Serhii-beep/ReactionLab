namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record BalanceRequest(IReadOnlyList<string> Reactants, IReadOnlyList<string> Products);

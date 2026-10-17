namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record BalanceRequest(IReadOnlyList<string> Reactants, IReadOnlyList<string> Products);

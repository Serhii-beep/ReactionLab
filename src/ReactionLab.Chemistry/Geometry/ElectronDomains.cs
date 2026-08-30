namespace ReactionLab.Chemistry.Geometry;

public readonly record struct ElectronDomains(int BondingDomains, int LonePairs)
{
    public int StericNumber => BondingDomains + LonePairs;

    public static ElectronDomains Around(
        int valenceElectrons,
        IReadOnlyList<int> bondOrders,
        int formalCharge)
    {
        var unshared = valenceElectrons - bondOrders.Sum() - formalCharge;

        if (unshared < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bondOrders), "The atom makes more bonds than it has electrons for.");
        }

        if (unshared % 2 != 0)
        {
            throw new ArgumentException(
                "An unpaired electron: radicals sit outside what VSEPR predicts.",
                nameof(valenceElectrons));
        }

        return new ElectronDomains(bondOrders.Count, unshared / 2);
    }
}

using System.Text;

namespace ReactionLab.Chemistry.Formulas;

public sealed class ChemicalComposition
{
    private readonly Dictionary<string, int> _bySymbol;

    internal ChemicalComposition(Dictionary<string, int> bySymbol, int charge)
    {
        _bySymbol = bySymbol;
        Charge = charge;
        Elements = HillOrder(bySymbol);
        Hill = Render(Elements);
    }

    public IReadOnlyList<ElementCount> Elements { get; }

    public int Charge { get; }

    public string Hill { get; }

    public int TotalAtoms => _bySymbol.Values.Sum();

    public int CountOf(string symbol) =>
        _bySymbol.TryGetValue(symbol, out var count) ? count : 0;

    private static List<ElementCount> HillOrder(Dictionary<string, int> tally)
    {
        var ordered = new List<ElementCount>();
        var remaining = new Dictionary<string, int>(tally, StringComparer.Ordinal);

        if (remaining.Remove("C", out var carbon))
        {
            ordered.Add(new ElementCount("C", carbon));

            if (remaining.Remove("H", out var hydrogen))
            {
                ordered.Add(new ElementCount("H", hydrogen));
            }
        }

        ordered.AddRange(remaining
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new ElementCount(pair.Key, pair.Value)));

        return ordered;
    }

    private static string Render(IReadOnlyList<ElementCount> elements)
    {
        var sb = new StringBuilder();

        foreach (var (symbol, count) in elements)
        {
            sb.Append(symbol);

            if (count > 1)
            {
                sb.Append(count);
            }
        }

        return sb.ToString();
    }
}

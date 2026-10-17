using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Browser.Contracts;
using ReactionLab.Chemistry.Formulas;
using ReactionLab.Chemistry.Geometry;

namespace ReactionLab.Chemistry.Browser;

[SupportedOSPlatform("browser")]
internal static partial class ChemistryExports
{
    private const string UnknownElement = "UnknownElement";

    private static Dictionary<string, decimal> _atomicMasses = new(StringComparer.Ordinal);
    private static Dictionary<string, AtomicGeometry> _atomicGeometry = new(StringComparer.Ordinal);

    [JSExport]
    internal static void LoadReferenceData(string atomicMassesJson, string atomicGeometryJson)
    {
        var masses = JsonSerializer.Deserialize(atomicMassesJson, ChemistryJsonContext.Default.DictionaryStringDecimal)
            ?? throw new JsonException("The atomic masses are missing.");
        var geometry = JsonSerializer.Deserialize(atomicGeometryJson, ChemistryJsonContext.Default.IReadOnlyListAtomicGeometryEntry)
            ?? throw new JsonException("The atomic geometry is missing.");

        _atomicMasses = new Dictionary<string, decimal>(masses, StringComparer.Ordinal);
        _atomicGeometry = geometry.ToDictionary(
            entry => entry.Symbol,
            entry => new AtomicGeometry(
                entry.ValenceElectrons,
                entry.SingleBondRadiusPicometers,
                entry.DoubleBondRadiusPicometers,
                entry.TripleBondRadiusPicometers,
                entry.AromaticBondRadiusPicometers),
            StringComparer.Ordinal);
    }

    [JSExport]
    internal static string ParseFormula(string formula)
    {
        var result = FormulaParser.TryParse(formula, out var composition, out var error)
            ? new FormulaResult(composition.Hill, composition.Charge, composition.Elements.ToDictionary(element => element.Symbol, element => element.Count, StringComparer.Ordinal), null)
            : new FormulaResult(null, 0, null, error.ToString());

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.FormulaResult);
    }

    [JSExport]
    internal static string MolarMassOf(string formula)
    {
        MolarMassResult result;

        if (!FormulaParser.TryParse(formula, out var composition, out var error))
        {
            result = new MolarMassResult(null, error.ToString(), null);
        }
        else if (MolarMass.TryCompute(composition, _atomicMasses, out var gramsPerMole, out var unknownSymbol))
        {
            result = new MolarMassResult(gramsPerMole, null, null);
        }
        else
        {
            result = new MolarMassResult(null, UnknownElement, unknownSymbol);
        }

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.MolarMassResult);
    }

    [JSExport]
    internal static string Balance(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.BalanceRequest)
            ?? throw new JsonException("The balance request is missing.");
        var reactants = new List<ChemicalComposition>();
        var products = new List<ChemicalComposition>();

        foreach (var (formulas, side) in new[] { (request.Reactants, reactants), (request.Products, products) })
        {
            foreach (var formula in formulas)
            {
                if (!FormulaParser.TryParse(formula, out var composition, out var parseError))
                {
                    return JsonSerializer.Serialize(new BalanceResult(null, null, parseError.ToString(), formula), ChemistryJsonContext.Default.BalanceResult);
                }

                side.Add(composition);
            }
        }

        var result = EquationBalancer.TryBalance(reactants, products, out var balanced, out var error)
            ? new BalanceResult(balanced.ReactantCoefficients, balanced.ProductCoefficients, null, null)
            : new BalanceResult(null, null, error.ToString(), null);

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.BalanceResult);
    }

    [JSExport]
    internal static string Assemble(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.SkeletonRequest)
            ?? throw new JsonException("The skeleton request is missing.");
        var atoms = request.Atoms.Select(atom => new SkeletalAtom(atom.Symbol, atom.FormalCharge)).ToList();
        var bonds = request.Bonds.Select(bond => new AtomBond(bond.From, bond.To, bond.Order)).ToList();

        var result = MoleculeAssembler.TryAssemble(atoms, bonds, _atomicGeometry, out var positions, out var error)
            ? new AssemblyResult([.. positions.SelectMany(position => new[] { position.X, position.Y, position.Z })], null)
            : new AssemblyResult(null, error.ToString());

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.AssemblyResult);
    }
}

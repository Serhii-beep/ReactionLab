using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

public class MoleculeSeeder : IDataSeeder
{
    private readonly ReactionLabDbContext _context;

    public MoleculeSeeder(ReactionLabDbContext context)
    {
        _context = context;
    }

    public int Order => 2;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Molecules.AnyAsync(cancellationToken))
        {
            return;
        }

        var elements = await _context.Elements.ToListAsync(cancellationToken);
        var hydrogen = elements.First(e => e.Symbol == "H");
        var oxygen = elements.First(e => e.Symbol == "O");
        var carbon = elements.First(e => e.Symbol == "C");
        var nitrogen = elements.First(e => e.Symbol == "N");
        var sodium = elements.First(e => e.Symbol == "Na");
        var chlorine = elements.First(e => e.Symbol == "Cl");

        var molecules = new List<Molecule>
        {
            new Molecule
            {
                Formula = "H2O",
                Name = "Water",
                IUPACName = "Oxidane",
                CommonNames = "[\"Water\", \"Dihydrogen monoxide\"]",
                MolecularWeight = 18.015m,
                IsOrganic = false,
                Category = "Inorganic",
                StateAtRoomTemp = MatterState.Liquid,
                Structure3D = GetWaterStructure(),
                Description = "Water is a transparent, tasteless, odorless, and nearly colorless chemical substance that is the main constituent of Earth's hydrosphere and the fluids of all known living organisms.",
                Uses = "[\"Drinking\", \"Irrigation\", \"Industrial solvent\", \"Cooling systems\"]",
                SafetyInfo = "Generally safe. Can cause drowning in large quantities.",
                InterestingFacts = "[\"Covers 71% of Earth's surface\", \"Is the only common substance to exist naturally in all three states of matter\", \"Has a high specific heat capacity\"]",
                MoleculeElements =
                {
                    new MoleculeElement { ElementId = hydrogen.Id, Count = 2 },
                    new MoleculeElement { ElementId = oxygen.Id, Count = 1 }
                }
            },
            new Molecule
            {
                Formula = "CO2",
                Name = "Carbon Dioxide",
                IUPACName = "Carbon dioxide",
                CommonNames = "[\"Carbonic acid gas\", \"Dry ice (solid form)\"]",
                MolecularWeight = 44.01m,
                IsOrganic = false,
                Category = "Inorganic",
                StateAtRoomTemp = MatterState.Gas,
                Structure3D = GetCO2Structure(),
                Description = "Carbon dioxide is a colorless gas with a density about 53% higher than that of dry air. It occurs naturally in Earth's atmosphere as a trace gas.",
                Uses = "[\"Carbonated beverages\", \"Fire extinguishers\", \"Refrigeration (dry ice)\", \"Plant photosynthesis\"]",
                SafetyInfo = "Can cause asphyxiation in high concentrations. Non-toxic at normal atmospheric levels.",
                InterestingFacts = "[\"Plants use it for photosynthesis\", \"Is a greenhouse gas\", \"Sublimes at -78.5°C at normal pressure\"]",
                MoleculeElements =
                {
                    new MoleculeElement { ElementId = carbon.Id, Count = 1 },
                    new MoleculeElement { ElementId = oxygen.Id, Count = 2 }
                }
            },
            new Molecule
            {
                Formula = "O2",
                Name = "Oxygen",
                IUPACName = "Dioxygen",
                CommonNames = "[\"Molecular oxygen\"]",
                MolecularWeight = 32.00m,
                IsOrganic = false,
                Category = "Inorganic",
                StateAtRoomTemp = MatterState.Gas,
                Structure3D = GetO2Structure(),
                Description = "Dioxygen is a colorless and odorless diatomic gas essential for aerobic respiration in most living organisms.",
                Uses = "[\"Respiration\", \"Medical oxygen therapy\", \"Welding\", \"Rocket propellant\"]",
                SafetyInfo = "Supports combustion. High concentrations can be toxic.",
                InterestingFacts = "[\"Makes up 21% of Earth's atmosphere\", \"Produced by photosynthesis\", \"Paramagnetic\"]",
                MoleculeElements =
                {
                    new MoleculeElement { ElementId = oxygen.Id, Count = 2 }
                }
            },
            new Molecule
            {
                Formula = "H2",
                Name = "Hydrogen",
                IUPACName = "Dihydrogen",
                CommonNames = "[\"Molecular hydrogen\"]",
                MolecularWeight = 2.016m,
                IsOrganic = false,
                Category = "Inorganic",
                StateAtRoomTemp = MatterState.Gas,
                Structure3D = GetH2Structure(),
                Description = "Dihydrogen is a colorless, odorless, tasteless, non-toxic, highly combustible diatomic gas.",
                Uses = "[\"Fuel cells\", \"Hydrogenation of fats\", \"Ammonia production\", \"Rocket fuel\"]",
                SafetyInfo = "Highly flammable. Forms explosive mixtures with air.",
                InterestingFacts = "[\"Lightest molecule\", \"Most abundant element in the universe\", \"Burns with an invisible flame\"]",
                MoleculeElements =
                {
                    new MoleculeElement { ElementId = hydrogen.Id, Count = 2 }
                }
            },
            new Molecule
            {
                Formula = "NaCl",
                Name = "Sodium Chloride",
                IUPACName = "Sodium chloride",
                CommonNames = "[\"Table salt\", \"Halite\", \"Rock salt\"]",
                MolecularWeight = 58.44m,
                IsOrganic = false,
                Category = "Ionic compound",
                StateAtRoomTemp = MatterState.Solid,
                Structure3D = GetNaClStructure(),
                Description = "Sodium chloride is an ionic compound with the chemical formula NaCl, representing a 1:1 ratio of sodium and chloride ions.",
                Uses = "[\"Food seasoning\", \"Food preservation\", \"De-icing roads\", \"Chemical production\"]",
                SafetyInfo = "Generally safe. Excessive consumption linked to hypertension.",
                InterestingFacts = "[\"Essential for human life\", \"Was used as currency in ancient times\", \"Forms cubic crystals\"]",
                MoleculeElements =
                {
                    new MoleculeElement { ElementId = sodium.Id, Count = 1 },
                    new MoleculeElement { ElementId = chlorine.Id, Count = 1 }
                }
            },
            new Molecule
            {
                Formula = "CH4",
                Name = "Methane",
                IUPACName = "Methane",
                CommonNames = "[\"Natural gas\", \"Marsh gas\"]",
                MolecularWeight = 16.04m,
                IsOrganic = true,
                Category = "Alkane",
                StateAtRoomTemp = MatterState.Gas,
                Structure3D = GetMethaneStructure(),
                Description = "Methane is the simplest alkane and the main constituent of natural gas.",
                Uses = "[\"Fuel for heating\", \"Electricity generation\", \"Hydrogen production\", \"Chemical feedstock\"]",
                SafetyInfo = "Highly flammable. Asphyxiant in high concentrations. Potent greenhouse gas.",
                InterestingFacts = "[\"Simplest hydrocarbon\", \"Produced by decomposition of organic matter\", \"Found on other planets\"]",
                MoleculeElements =
                {
                    new MoleculeElement { ElementId = carbon.Id, Count = 1 },
                    new MoleculeElement { ElementId = hydrogen.Id, Count = 4 }
                }
            },
            new Molecule
            {
                Formula = "NH3",
                Name = "Ammonia",
                IUPACName = "Azane",
                CommonNames = "[\"Ammonia\", \"Hydrogen nitride\"]",
                MolecularWeight = 17.031m,
                IsOrganic = false,
                Category = "Inorganic",
                StateAtRoomTemp = MatterState.Gas,
                Structure3D = GetAmmoniaStructure(),
                Description = "Ammonia is a compound of nitrogen and hydrogen with the formula NH3. It is a colorless gas with a characteristic pungent smell.",
                Uses = "[\"Fertilizer production\", \"Cleaning products\", \"Refrigeration\", \"Chemical synthesis\"]",
                SafetyInfo = "Toxic if inhaled. Corrosive. Causes severe burns.",
                InterestingFacts = "[\"Has a distinctive sharp smell\", \"Essential for the nitrogen cycle\", \"Used in smelling salts\"]",
                MoleculeElements =
                {
                    new MoleculeElement { ElementId = nitrogen.Id, Count = 1 },
                    new MoleculeElement { ElementId = hydrogen.Id, Count = 3 }
                }
            },
            new Molecule
            {
                Formula = "C2H6",
                Name = "Ethane",
                IUPACName = "Ethane",
                CommonNames = "[\"Ethane\", \"Methylmethane\"]",
                MolecularWeight = 30.07m,
                IsOrganic = true,
                Category = "Alkane",
                StateAtRoomTemp = MatterState.Gas,
                Structure3D = GetEthaneStructure(),
                Description = "Ethane is an organic chemical compound with the chemical formula C2H6. It is a colorless, odorless gas.",
                Uses = "[\"Fuel\", \"Petrochemical feedstock\", \"Ethylene production\"]",
                SafetyInfo = "Highly flammable. Asphyxiant in high concentrations.",
                InterestingFacts = "[\"Second simplest alkane\", \"Component of natural gas\", \"Found in Titan's atmosphere\"]",
                MoleculeElements =
                {
                    new MoleculeElement { ElementId = carbon.Id, Count = 2 },
                    new MoleculeElement { ElementId = hydrogen.Id, Count = 6 }
                }
            }
        };

        await _context.Molecules.AddRangeAsync(molecules, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Water: H2O - Bent structure (104.5° angle)
    private static string GetWaterStructure()
    {
        return """
        {
            "atoms": [
                { "symbol": "O", "position": [0, 0, 0] },
                { "symbol": "H", "position": [0.757, 0.586, 0] },
                { "symbol": "H", "position": [-0.757, 0.586, 0] }
            ],
            "bonds": [
                { "from": 0, "to": 1, "type": "covalent" },
                { "from": 0, "to": 2, "type": "covalent" }
            ]
        }
        """;
    }

    // Carbon Dioxide: CO2 - Linear structure
    private static string GetCO2Structure()
    {
        return """
        {
            "atoms": [
                { "symbol": "O", "position": [-1.16, 0, 0] },
                { "symbol": "C", "position": [0, 0, 0] },
                { "symbol": "O", "position": [1.16, 0, 0] }
            ],
            "bonds": [
                { "from": 0, "to": 1, "type": "double" },
                { "from": 1, "to": 2, "type": "double" }
            ]
        }
        """;
    }

    // Dioxygen: O2 - Linear
    private static string GetO2Structure()
    {
        return """
        {
            "atoms": [
                { "symbol": "O", "position": [-0.6, 0, 0] },
                { "symbol": "O", "position": [0.6, 0, 0] }
            ],
            "bonds": [
                { "from": 0, "to": 1, "type": "double" }
            ]
        }
        """;
    }

    // Dihydrogen: H2 - Linear
    private static string GetH2Structure()
    {
        return """
        {
            "atoms": [
                { "symbol": "H", "position": [-0.37, 0, 0] },
                { "symbol": "H", "position": [0.37, 0, 0] }
            ],
            "bonds": [
                { "from": 0, "to": 1, "type": "covalent" }
            ]
        }
        """;
    }

    // Sodium Chloride: NaCl - Ion pair (simplified)
    private static string GetNaClStructure()
    {
        return """
        {
            "atoms": [
                { "symbol": "Na", "position": [-1.0, 0, 0] },
                { "symbol": "Cl", "position": [1.0, 0, 0] }
            ],
            "bonds": [
                { "from": 0, "to": 1, "type": "ionic" }
            ]
        }
        """;
    }

    // Methane: CH4 - Tetrahedral structure
    private static string GetMethaneStructure()
    {
        return """
        {
            "atoms": [
                { "symbol": "C", "position": [0, 0, 0] },
                { "symbol": "H", "position": [0.629, 0.629, 0.629] },
                { "symbol": "H", "position": [-0.629, -0.629, 0.629] },
                { "symbol": "H", "position": [-0.629, 0.629, -0.629] },
                { "symbol": "H", "position": [0.629, -0.629, -0.629] }
            ],
            "bonds": [
                { "from": 0, "to": 1, "type": "covalent" },
                { "from": 0, "to": 2, "type": "covalent" },
                { "from": 0, "to": 3, "type": "covalent" },
                { "from": 0, "to": 4, "type": "covalent" }
            ]
        }
        """;
    }

    // Ammonia: NH3 - Trigonal pyramidal structure
    private static string GetAmmoniaStructure()
    {
        return """
        {
            "atoms": [
                { "symbol": "N", "position": [0, 0, 0.11] },
                { "symbol": "H", "position": [0.94, 0, -0.26] },
                { "symbol": "H", "position": [-0.47, 0.81, -0.26] },
                { "symbol": "H", "position": [-0.47, -0.81, -0.26] }
            ],
            "bonds": [
                { "from": 0, "to": 1, "type": "covalent" },
                { "from": 0, "to": 2, "type": "covalent" },
                { "from": 0, "to": 3, "type": "covalent" }
            ]
        }
        """;
    }

    // Ethane: C2H6 - Two tetrahedral carbons
    private static string GetEthaneStructure()
    {
        return """
        {
            "atoms": [
                { "symbol": "C", "position": [-0.762, 0, 0] },
                { "symbol": "C", "position": [0.762, 0, 0] },
                { "symbol": "H", "position": [-1.156, 0.892, 0.515] },
                { "symbol": "H", "position": [-1.156, -0.892, 0.515] },
                { "symbol": "H", "position": [-1.156, 0, -1.03] },
                { "symbol": "H", "position": [1.156, 0.892, -0.515] },
                { "symbol": "H", "position": [1.156, -0.892, -0.515] },
                { "symbol": "H", "position": [1.156, 0, 1.03] }
            ],
            "bonds": [
                { "from": 0, "to": 1, "type": "covalent" },
                { "from": 0, "to": 2, "type": "covalent" },
                { "from": 0, "to": 3, "type": "covalent" },
                { "from": 0, "to": 4, "type": "covalent" },
                { "from": 1, "to": 5, "type": "covalent" },
                { "from": 1, "to": 6, "type": "covalent" },
                { "from": 1, "to": 7, "type": "covalent" }
            ]
        }
        """;
    }
}
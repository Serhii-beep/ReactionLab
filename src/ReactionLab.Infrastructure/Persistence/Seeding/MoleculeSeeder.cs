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
                Description = "Methane is the simplest alkane and the main constituent of natural gas.",
                Uses = "[\"Fuel for heating\", \"Electricity generation\", \"Hydrogen production\", \"Chemical feedstock\"]",
                SafetyInfo = "Highly flammable. Asphyxiant in high concentrations. Potent greenhouse gas.",
                InterestingFacts = "[\"Simplest hydrocarbon\", \"Produced by decomposition of organic matter\", \"Found on other planets\"]",
                MoleculeElements =
                {
                    new MoleculeElement { ElementId = carbon.Id, Count = 1 },
                    new MoleculeElement { ElementId = hydrogen.Id, Count = 4 }
                }
            }
        };

        await _context.Molecules.AddRangeAsync(molecules, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

public class ReactionSeeder : IDataSeeder
{
    private readonly ReactionLabDbContext _context;

    public ReactionSeeder(ReactionLabDbContext context)
    {
        _context = context;
    }

    public int Order => 3;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Reactions.AnyAsync(cancellationToken))
        {
            return;
        }

        var molecules = await _context.Molecules.ToListAsync(cancellationToken);
        var water = molecules.First(m => m.Formula == "H2O");
        var carbonDioxide = molecules.First(m => m.Formula == "CO2");
        var oxygen = molecules.First(m => m.Formula == "O2");
        var hydrogen = molecules.First(m => m.Formula == "H2");
        var methane = molecules.First(m => m.Formula == "CH4");

        var tags = new List<Tag>
        {
            new Tag { Name = "Combustion", Category = "reaction_type" },
            new Tag { Name = "Synthesis", Category = "reaction_type" },
            new Tag { Name = "Decomposition", Category = "reaction_type" },
            new Tag { Name = "Exothermic", Category = "thermodynamics" },
            new Tag { Name = "Endothermic", Category = "thermodynamics" },
            new Tag { Name = "Beginner", Category = "difficulty" },
            new Tag { Name = "Common", Category = "frequency" }
        };

        await _context.Tags.AddRangeAsync(tags, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var combustionTag = tags.First(t => t.Name == "Combustion");
        var synthesisTag = tags.First(t => t.Name == "Synthesis");
        var decompositionTag = tags.First(t => t.Name == "Decomposition");
        var exothermicTag = tags.First(t => t.Name == "Exothermic");
        var endothermicTag = tags.First(t => t.Name == "Endothermic");
        var beginnerTag = tags.First(t => t.Name == "Beginner");
        var commonTag = tags.First(t => t.Name == "Common");

        var reactions = new List<Reaction>
        {
            new Reaction
            {
                Name = "Synthesis of Water",
                Equation = "2H2 + O2 → 2H2O",
                EquationBalanced = "2H2 + O2 → 2H2O",
                ReactionType = ReactionType.Synthesis,
                Category = "Synthesis",
                RequiredTemperature = 811m,
                RequiresCatalyst = false,
                EnthalpyChange = -572m,
                IsExothermic = true,
                ActivationEnergy = 75m,
                AnimationType = "combustion",
                EffectPreset = "fire",
                AnimationDurationMs = 3000,
                Description = "The synthesis of water from hydrogen and oxygen is a highly exothermic reaction. When ignited, hydrogen gas combines with oxygen to form water vapor, releasing significant energy.",
                Mechanism = "Hydrogen molecules collide with oxygen molecules. At sufficient temperature, bonds break and reform to create water molecules.",
                RealWorldExamples = "[\"Hydrogen fuel cells\", \"Rocket propulsion\", \"Oxyhydrogen welding\"]",
                SafetyWarnings = "Highly explosive when hydrogen and oxygen are mixed. Keep away from ignition sources.",
                DifficultyLevel = 1,
                Participants =
                {
                    new ReactionParticipant { MoleculeId = hydrogen.Id, Role = ParticipantRole.Reactant, Coefficient = 2, State = MatterState.Gas },
                    new ReactionParticipant { MoleculeId = oxygen.Id, Role = ParticipantRole.Reactant, Coefficient = 1, State = MatterState.Gas },
                    new ReactionParticipant { MoleculeId = water.Id, Role = ParticipantRole.Product, Coefficient = 2, State = MatterState.Gas }
                },
                ReactionTags =
                {
                    new ReactionTag { TagId = synthesisTag.Id },
                    new ReactionTag { TagId = exothermicTag.Id },
                    new ReactionTag { TagId = beginnerTag.Id },
                    new ReactionTag { TagId = commonTag.Id }
                }
            },

            new Reaction
            {
                Name = "Combustion of Methane",
                Equation = "CH4 + 2O2 → CO2 + 2H2O",
                EquationBalanced = "CH4 + 2O2 → CO2 + 2H2O",
                ReactionType = ReactionType.Combustion,
                Category = "Combustion",
                RequiredTemperature = 873m,
                RequiresCatalyst = false,
                EnthalpyChange = -890m,
                IsExothermic = true,
                ActivationEnergy = 150m,
                AnimationType = "combustion",
                EffectPreset = "fire",
                AnimationDurationMs = 4000,
                Description = "The combustion of methane is the primary reaction in natural gas burning. It produces carbon dioxide and water while releasing substantial heat energy.",
                Mechanism = "Methane reacts with oxygen at high temperature, breaking C-H and O=O bonds, forming C=O and O-H bonds in the products.",
                RealWorldExamples = "[\"Natural gas stoves\", \"Gas furnaces\", \"Power plants\", \"Gas water heaters\"]",
                SafetyWarnings = "Ensure proper ventilation. Incomplete combustion produces toxic carbon monoxide.",
                DifficultyLevel = 2,
                Participants =
                {
                    new ReactionParticipant { MoleculeId = methane.Id, Role = ParticipantRole.Reactant, Coefficient = 1, State = MatterState.Gas },
                    new ReactionParticipant { MoleculeId = oxygen.Id, Role = ParticipantRole.Reactant, Coefficient = 2, State = MatterState.Gas },
                    new ReactionParticipant { MoleculeId = carbonDioxide.Id, Role = ParticipantRole.Product, Coefficient = 1, State = MatterState.Gas },
                    new ReactionParticipant { MoleculeId = water.Id, Role = ParticipantRole.Product, Coefficient = 2, State = MatterState.Gas }
                },
                ReactionTags =
                {
                    new ReactionTag { TagId = combustionTag.Id },
                    new ReactionTag { TagId = exothermicTag.Id },
                    new ReactionTag { TagId = commonTag.Id }
                }
            },

            new Reaction
            {
                Name = "Electrolysis of Water",
                Equation = "2H2O → 2H2 + O2",
                EquationBalanced = "2H2O → 2H2 + O2",
                ReactionType = ReactionType.Decomposition,
                Category = "Decomposition",
                RequiredTemperature = 298m,
                RequiresCatalyst = true,
                CatalystInfo = "Requires electrical current. Electrolytes like NaOH or H2SO4 improve conductivity.",
                EnthalpyChange = 572m,
                IsExothermic = false,
                ActivationEnergy = 285m,
                AnimationType = "electrolysis",
                EffectPreset = "bubbles",
                AnimationDurationMs = 5000,
                Description = "Electrolysis of water splits water molecules into hydrogen and oxygen gases using electrical energy. This is the reverse of the synthesis of water.",
                Mechanism = "Electrical current breaks the O-H bonds in water molecules. Hydrogen ions gain electrons at the cathode, oxygen ions lose electrons at the anode.",
                RealWorldExamples = "[\"Hydrogen production for fuel cells\", \"Laboratory oxygen generation\", \"Industrial hydrogen production\"]",
                SafetyWarnings = "Produces flammable hydrogen gas. Ensure proper ventilation and no ignition sources.",
                DifficultyLevel = 2,
                Participants =
                {
                    new ReactionParticipant { MoleculeId = water.Id, Role = ParticipantRole.Reactant, Coefficient = 2, State = MatterState.Liquid },
                    new ReactionParticipant { MoleculeId = hydrogen.Id, Role = ParticipantRole.Product, Coefficient = 2, State = MatterState.Gas },
                    new ReactionParticipant { MoleculeId = oxygen.Id, Role = ParticipantRole.Product, Coefficient = 1, State = MatterState.Gas }
                },
                ReactionTags =
                {
                    new ReactionTag { TagId = decompositionTag.Id },
                    new ReactionTag { TagId = endothermicTag.Id },
                    new ReactionTag { TagId = beginnerTag.Id }
                }
            }
        };

        await _context.Reactions.AddRangeAsync(reactions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
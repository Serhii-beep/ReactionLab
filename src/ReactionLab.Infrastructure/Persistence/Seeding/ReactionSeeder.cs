using System.Text.Json;
using System.Text.Json.Serialization;
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
        if (await _context.Tags.AnyAsync(cancellationToken))
        {
            // If tags exist, we assume seeding already happened or at least tags are there.
            // But reactions might not be there. Let's check reactions.
            if (await _context.Reactions.AnyAsync(cancellationToken))
            {
                return;
            }
        }
        else
        {
            await SeedTagsAsync(cancellationToken);
        }

        await SeedReactionsAsync(cancellationToken);
    }

    private async Task SeedTagsAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seeding", "Data", "tags.json");

        if (!File.Exists(filePath))
        {
            filePath = Path.Combine("ReactionLab.Infrastructure", "Persistence", "Seeding", "Data", "tags.json");
        }

        if (!File.Exists(filePath))
        {
            filePath = Path.Combine("..", "ReactionLab.Infrastructure", "Persistence", "Seeding", "Data", "tags.json");
        }

        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var tags = JsonSerializer.Deserialize<List<Tag>>(json);
            if (tags != null)
            {
                await _context.Tags.AddRangeAsync(tags, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task SeedReactionsAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seeding", "Data", "reactions.json");

        if (!File.Exists(filePath))
        {
            filePath = Path.Combine("ReactionLab.Infrastructure", "Persistence", "Seeding", "Data", "reactions.json");
        }

        if (!File.Exists(filePath))
        {
            filePath = Path.Combine("..", "ReactionLab.Infrastructure", "Persistence", "Seeding", "Data", "reactions.json");
        }

        if (!File.Exists(filePath))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var reactionData = JsonSerializer.Deserialize<List<ReactionSeedDto>>(json, options);
        var molecules = await _context.Molecules.ToListAsync(cancellationToken);
        var tags = await _context.Tags.ToListAsync(cancellationToken);

        if (reactionData != null)
        {
            foreach (var dto in reactionData)
            {
                var reaction = new Reaction
                {
                    Name = dto.Name,
                    Equation = dto.Equation,
                    EquationBalanced = dto.EquationBalanced,
                    ReactionType = Enum.Parse<ReactionType>(dto.ReactionType),
                    Category = dto.Category,
                    RequiredTemperature = dto.RequiredTemperature,
                    RequiresCatalyst = dto.RequiresCatalyst,
                    CatalystInfo = dto.CatalystInfo,
                    EnthalpyChange = dto.EnthalpyChange,
                    IsExothermic = dto.IsExothermic,
                    ActivationEnergy = dto.ActivationEnergy,
                    AnimationType = dto.AnimationType,
                    EffectPreset = dto.EffectPreset,
                    AnimationDurationMs = dto.AnimationDurationMs,
                    Description = dto.Description,
                    Mechanism = dto.Mechanism,
                    RealWorldExamples = JsonSerializer.Serialize(dto.RealWorldExamples),
                    SafetyWarnings = dto.SafetyWarnings,
                    DifficultyLevel = dto.DifficultyLevel
                };

                foreach (var pDto in dto.Participants)
                {
                    var molecule = molecules.FirstOrDefault(m => m.Formula == pDto.MoleculeFormula);
                    if (molecule != null)
                    {
                        reaction.Participants.Add(new ReactionParticipant
                        {
                            MoleculeId = molecule.Id,
                            Role = Enum.Parse<ParticipantRole>(pDto.Role),
                            Coefficient = pDto.Coefficient,
                            State = Enum.Parse<MatterState>(pDto.State)
                        });
                    }
                }

                foreach (var tagName in dto.Tags)
                {
                    var tag = tags.FirstOrDefault(t => t.Name == tagName);
                    if (tag != null)
                    {
                        reaction.ReactionTags.Add(new ReactionTag { TagId = tag.Id });
                    }
                }

                await _context.Reactions.AddAsync(reaction, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private class ReactionSeedDto
    {
        public string Name { get; set; } = string.Empty;
        public string Equation { get; set; } = string.Empty;
        public string EquationBalanced { get; set; } = string.Empty;
        public string ReactionType { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal? RequiredTemperature { get; set; }
        public bool RequiresCatalyst { get; set; }
        public string? CatalystInfo { get; set; }
        public decimal? EnthalpyChange { get; set; }
        public bool IsExothermic { get; set; }
        public decimal? ActivationEnergy { get; set; }
        public string? AnimationType { get; set; }
        public string? EffectPreset { get; set; }
        public int AnimationDurationMs { get; set; }
        public string? Description { get; set; }
        public string? Mechanism { get; set; }
        public List<string> RealWorldExamples { get; set; } = [];
        public string? SafetyWarnings { get; set; }
        public int DifficultyLevel { get; set; }
        public List<ReactionParticipantDto> Participants { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    private class ReactionParticipantDto
    {
        public string MoleculeFormula { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int Coefficient { get; set; }
        public string State { get; set; } = string.Empty;
    }
}

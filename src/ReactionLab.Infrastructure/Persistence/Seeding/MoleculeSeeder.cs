using System.Text.Json;
using System.Text.Json.Serialization;
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

        var molecules = await GetMoleculesAsync(cancellationToken);
        if (molecules != null && molecules.Any())
        {
            await _context.Molecules.AddRangeAsync(molecules, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<List<Molecule>> GetMoleculesAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seeding", "Data", "molecules.json");
        
        if (!File.Exists(filePath))
        {
            filePath = Path.Combine("ReactionLab.Infrastructure", "Persistence", "Seeding", "Data", "molecules.json");
        }

        if (!File.Exists(filePath))
        {
            filePath = Path.Combine("..", "ReactionLab.Infrastructure", "Persistence", "Seeding", "Data", "molecules.json");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Seed data file not found", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var moleculeData = JsonSerializer.Deserialize<List<MoleculeSeedDto>>(json, options);
        var elements = await _context.Elements.ToListAsync(cancellationToken);
        
        var result = new List<Molecule>();

        if (moleculeData != null)
        {
            foreach (var dto in moleculeData)
            {
                var molecule = new Molecule
                {
                    Formula = dto.Formula,
                    Name = dto.Name,
                    IUPACName = dto.IUPACName,
                    CommonNames = JsonSerializer.Serialize(dto.CommonNames),
                    MolecularWeight = dto.MolecularWeight,
                    IsOrganic = dto.IsOrganic,
                    Category = dto.Category,
                    StateAtRoomTemp = Enum.Parse<MatterState>(dto.StateAtRoomTemp),
                    Structure3D = dto.Structure3D,
                    Description = dto.Description,
                    Uses = JsonSerializer.Serialize(dto.Uses),
                    SafetyInfo = dto.SafetyInfo,
                    InterestingFacts = JsonSerializer.Serialize(dto.InterestingFacts)
                };

                foreach (var elDto in dto.Elements)
                {
                    var element = elements.FirstOrDefault(e => e.Symbol == elDto.Symbol);
                    if (element != null)
                    {
                        molecule.MoleculeElements.Add(new MoleculeElement
                        {
                            ElementId = element.Id,
                            Count = elDto.Count
                        });
                    }
                }

                result.Add(molecule);
            }
        }

        return result;
    }

    private class MoleculeSeedDto
    {
        public string Formula { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? IUPACName { get; set; }
        public List<string> CommonNames { get; set; } = [];
        public decimal? MolecularWeight { get; set; }
        public bool IsOrganic { get; set; }
        public string? Category { get; set; }
        public string StateAtRoomTemp { get; set; } = string.Empty;
        public string? Structure3D { get; set; }
        public string? Description { get; set; }
        public List<string> Uses { get; set; } = [];
        public string? SafetyInfo { get; set; }
        public List<string> InterestingFacts { get; set; } = [];
        public List<MoleculeElementDto> Elements { get; set; } = [];
    }

    private class MoleculeElementDto
    {
        public string Symbol { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

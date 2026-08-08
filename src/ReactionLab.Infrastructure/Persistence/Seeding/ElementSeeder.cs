using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

public class ElementSeeder : IDataSeeder
{
    private readonly ReactionLabDbContext _context;

    public ElementSeeder(ReactionLabDbContext context)
    {
        _context = context;
    }

    public int Order => 1;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Elements.AnyAsync(cancellationToken))
        {
            return;
        }

        var elements = await GetElementsAsync();
        if (elements != null && elements.Any())
        {
            await _context.Elements.AddRangeAsync(elements, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<List<Element>> GetElementsAsync()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seeding", "Data", "elements.json");

        if (!File.Exists(filePath))
        {
            // Fallback for development if not copied to output yet
            filePath = Path.Combine("ReactionLab.Infrastructure", "Persistence", "Seeding", "Data", "elements.json");
        }

        if (!File.Exists(filePath))
        {
            filePath = Path.Combine("..", "ReactionLab.Infrastructure", "Persistence", "Seeding", "Data", "elements.json");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Seed data file not found", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var elementData = JsonSerializer.Deserialize<List<ElementSeedDto>>(json, options);

        return elementData?.Select(d => d.ToEntity()).ToList() ?? [];
    }

    private class ElementSeedDto
    {
        public int AtomicNumber { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal AtomicMass { get; set; }
        public string Category { get; set; } = string.Empty;
        public int Period { get; set; }
        public int? Group { get; set; }
        public string ElectronConfiguration { get; set; } = string.Empty;
        public decimal? Electronegativity { get; set; }
        public decimal? AtomicRadius { get; set; }
        public decimal? IonizationEnergy { get; set; }
        public decimal? MeltingPoint { get; set; }
        public decimal? BoilingPoint { get; set; }
        public decimal? Density { get; set; }
        public string Color { get; set; } = string.Empty;
        public string StateAtRoomTemp { get; set; } = string.Empty;
        public string DisplayColor { get; set; } = string.Empty;
        public decimal Radius3D { get; set; }
        public string DiscoveryInfo { get; set; } = string.Empty;
        public List<string> InterestingFacts { get; set; } = [];

        public Element ToEntity()
        {
            return new Element
            {
                AtomicNumber = AtomicNumber,
                Symbol = Symbol,
                Name = Name,
                AtomicMass = AtomicMass,
                Category = Enum.Parse<Domain.Enums.ElementCategory>(Category),
                Period = Period,
                Group = Group,
                ElectronConfiguration = ElectronConfiguration,
                Electronegativity = Electronegativity,
                AtomicRadius = AtomicRadius,
                IonizationEnergy = IonizationEnergy,
                MeltingPoint = MeltingPoint,
                BoilingPoint = BoilingPoint,
                Density = Density,
                Color = Color,
                StateAtRoomTemp = Enum.Parse<Domain.Enums.MatterState>(StateAtRoomTemp),
                DisplayColor = DisplayColor,
                Radius3D = Radius3D,
                DiscoveryInfo = DiscoveryInfo,
                InterestingFacts = JsonSerializer.Serialize(InterestingFacts)
            };
        }
    }
}

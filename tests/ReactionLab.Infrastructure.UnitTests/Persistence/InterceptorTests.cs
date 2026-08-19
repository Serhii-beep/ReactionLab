using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Substances;
using ReactionLab.Infrastructure.Persistence;
using ReactionLab.Infrastructure.Persistence.Interceptors;
using Shouldly;
using Xunit;

namespace ReactionLab.Infrastructure.UnitTests.Persistence;

public sealed class InterceptorTests
{
    [Fact]
    public void SearchText_SpansEveryLocaleAndBothFormulaForms()
    {
        var text = SearchProjectionInterceptor.SearchTextFor(Water());

        text.ShouldContain("Water");
        text.ShouldContain("Translated");
        text.ShouldContain("H2O");
        text.ShouldContain("Oxidane");
        text.ShouldContain("Aqua");
    }

    [Fact]
    public void SearchText_DeduplicatesRepeatedTerms() =>
        SearchProjectionInterceptor.SearchTextFor(Water())
            .Split(' ').Count(part => part == "H2O").ShouldBe(1);

    [Fact]
    public void Project_FillsTheSearchColumnOnAddedAggregate()
    {
        using var context = CreateContext();
        var substance = Water();
        context.Add(substance);

        SearchProjectionInterceptor.Project(context);

        context.Entry(substance).Property<string>("search_text")
            .CurrentValue.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Audit_StampsBothTimestampsOnInsert()
    {
        using var context = CreateContext();
        var now = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
        var substance = Water();
        context.Add(substance);

        new AuditInterceptor(new Clock(now)).Stamp(context);

        var entry = context.Entry(substance);
        entry.Property("created_at").CurrentValue.ShouldBe(now);
        entry.Property("updated_at").CurrentValue.ShouldBe(now);
    }

    [Fact]
    public void Audit_DoesNotStampCreatedAtOnUpdate()
    {
        using var context = CreateContext();
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var modified = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

        var substance = Water();
        substance.ClearDomainEvents();
        context.Attach(substance);
        context.Entry(substance).Property("created_at").CurrentValue = created;

        substance.Classify("Inorganic");
        context.ChangeTracker.DetectChanges();
        new AuditInterceptor(new Clock(modified)).Stamp(context);

        var entry = context.Entry(substance);
        entry.Property("created_at").CurrentValue.ShouldBe(created);
        entry.Property("updated_at").CurrentValue.ShouldBe(modified);
    }

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=localhost;Database=reactionlab;Username=none;Password=none")
                .Options);

    private static Substance Water()
    {
        var substance = Substance.Create(
            ChemicalFormula.Create("H2O").Value,
            SubstanceContent.Create("Water", "Oxidane", commonNames: ["Aqua"]).Value,
            SubstanceKind.Molecular,
            isOrganic: false,
            MatterState.Liquid).Value;

        substance.Translate(SupportedLocale.Ukrainian, SubstanceContent.Create("Translated").Value);

        return substance;
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

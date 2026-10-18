using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Domain.Common;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Domain.Enums;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Domain.Reference;
using ReactionLab.Catalog.Domain.Releases;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.Catalog.Infrastructure.Persistence.Converters;

namespace ReactionLab.Catalog.Infrastructure.Persistence;

internal static class DomainConversions
{
    public static ModelConfigurationBuilder ApplyDomainConversions(this ModelConfigurationBuilder builder)
    {
        builder.Properties<ElementId>().HaveConversion<StronglyTypedIdConverter<ElementId>>();
        builder.Properties<SubstanceId>().HaveConversion<StronglyTypedIdConverter<SubstanceId>>();
        builder.Properties<ReactionId>().HaveConversion<StronglyTypedIdConverter<ReactionId>>();
        builder.Properties<ReactionParticipantId>().HaveConversion<StronglyTypedIdConverter<ReactionParticipantId>>();
        builder.Properties<ChemistryReferenceId>().HaveConversion<StronglyTypedIdConverter<ChemistryReferenceId>>();
        builder.Properties<CatalogReleaseId>().HaveConversion<StronglyTypedIdConverter<CatalogReleaseId>>();

        builder.Properties<ElementSymbol>().HaveConversion<ElementSymbolConverter>().HaveMaxLength(3);
        builder.Properties<AtomicNumber>().HaveConversion<AtomicNumberConverter>();
        builder.Properties<AtomicMass>().HaveConversion<AtomicMassConverter>().HavePrecision(10, 5);
        builder.Properties<Electronegativity>().HaveConversion<ElectronegativityConverter>().HavePrecision(4, 2);
        builder.Properties<HexColor>().HaveConversion<HexColorConverter>().HaveMaxLength(7);
        builder.Properties<Temperature>().HaveConversion<TemperatureConverter>().HavePrecision(12, 4);
        builder.Properties<Pressure>().HaveConversion<PressureConverter>().HavePrecision(12, 4);
        builder.Properties<Enthalpy>().HaveConversion<EnthalpyConverter>().HavePrecision(12, 4);
        builder.Properties<ChemicalFormula>().HaveConversion<ChemicalFormulaConverter>().HaveMaxLength(ChemicalFormula.MaximumLength);
        builder.Properties<MolecularWeight>().HaveConversion<MolecularWeightConverter>().HavePrecision(12, 5);
        builder.Properties<DifficultyLevel>().HaveConversion<DifficultyLevelConverter>();
        builder.Properties<ReferenceKey>().HaveConversion<ReferenceKeyConverter>().HaveMaxLength(ReferenceKey.MaximumLength);

        builder.Properties<ElementCategory>().HaveConversion<string>().HaveMaxLength(40);
        builder.Properties<MatterState>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<SubstanceKind>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<ParticipantRole>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<ReactionType>().HaveConversion<string>().HaveMaxLength(40);

        return builder;
    }
}

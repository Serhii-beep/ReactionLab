using System.Text.Json.Serialization;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ChemistryReferenceDocument(
    [property: JsonPropertyName("atomic-geometry")] AtomicGeometryDataset AtomicGeometry,
    [property: JsonPropertyName("ions")] IonDataset Ions,
    [property: JsonPropertyName("activity-series")] ActivitySeriesDataset ActivitySeries,
    [property: JsonPropertyName("thermodynamics")] ThermodynamicsDataset Thermodynamics);

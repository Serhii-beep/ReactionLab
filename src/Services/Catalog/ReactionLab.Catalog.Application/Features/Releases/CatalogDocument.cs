using System.Text.Json;
using ReactionLab.Catalog.Application.Features.Elements.Contracts;
using ReactionLab.Catalog.Application.Features.Reactions.Contracts;
using ReactionLab.Catalog.Application.Features.Substances.Contracts;

namespace ReactionLab.Catalog.Application.Features.Releases;

public sealed record CatalogDocument(
    IReadOnlyList<ElementResponse> Elements,
    IReadOnlyList<SubstanceResponse> Substances,
    IReadOnlyList<ReactionResponse> Reactions,
    JsonElement Reference);

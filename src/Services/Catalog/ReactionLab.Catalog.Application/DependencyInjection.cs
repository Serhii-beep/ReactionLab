using Microsoft.Extensions.DependencyInjection;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Features.Chemistry.GetChemistryReference;
using ReactionLab.Catalog.Application.Features.Elements.GetElementById;
using ReactionLab.Catalog.Application.Features.Elements.GetElementBySymbol;
using ReactionLab.Catalog.Application.Features.Elements.ListElements;
using ReactionLab.Catalog.Application.Features.Elements.TranslateElement;
using ReactionLab.Catalog.Application.Features.Reactions.GetReactionById;
using ReactionLab.Catalog.Application.Features.Reactions.ListReactions;
using ReactionLab.Catalog.Application.Features.Releases.Reads;
using ReactionLab.Catalog.Application.Features.Substances.GetSubstanceById;
using ReactionLab.Catalog.Application.Features.Substances.ListSubstances;

namespace ReactionLab.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CacheInvalidator>();
        services.AddScoped<CatalogCacheScope>();

        services.AddElementHandlers();
        services.AddSubstanceHandlers();
        services.AddReactionHandlers();
        services.AddChemistryHandlers();
        services.AddReleaseHandlers();

        return services;
    }

    private static IServiceCollection AddElementHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetElementByIdHandler>();
        services.AddScoped<TranslateElementHandler>();
        services.AddScoped<ListElementsHandler>();
        services.AddScoped<GetElementBySymbolHandler>();

        return services;
    }

    private static IServiceCollection AddSubstanceHandlers(this IServiceCollection services)
    {
        services.AddScoped<ListSubstancesHandler>();
        services.AddScoped<GetSubstanceByIdHandler>();

        return services;
    }

    private static IServiceCollection AddReactionHandlers(this IServiceCollection services)
    {
        services.AddScoped<ListReactionsHandler>();
        services.AddScoped<GetReactionByIdHandler>();

        return services;
    }

    private static IServiceCollection AddReleaseHandlers(this IServiceCollection services)
    {
        services.AddScoped<SearchReleaseSubstancesHandler>();
        services.AddScoped<GetReleaseSubstanceHandler>();
        services.AddScoped<FindReleaseSubstancesByStructureHandler>();
        services.AddScoped<MatchReleaseReactionsHandler>();
        services.AddScoped<GetReleaseReactionHandler>();

        return services;
    }

    private static IServiceCollection AddChemistryHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetChemistryReferenceHandler>();

        return services;
    }
}

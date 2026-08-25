using Microsoft.Extensions.DependencyInjection;
using ReactionLab.Application.Features.Elements.GetElementById;
using ReactionLab.Application.Features.Elements.GetElementBySymbol;
using ReactionLab.Application.Features.Elements.ListElements;
using ReactionLab.Application.Features.Elements.TranslateElement;
using ReactionLab.Application.Features.Reactions.GetReactionById;
using ReactionLab.Application.Features.Reactions.ListReactions;
using ReactionLab.Application.Features.Substances.GetSubstanceById;
using ReactionLab.Application.Features.Substances.ListSubstances;

namespace ReactionLab.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddElementHandlers();
        services.AddSubstanceHandlers();
        services.AddReactionHandlers();

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
}

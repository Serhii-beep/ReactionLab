using Microsoft.Extensions.DependencyInjection;
using ReactionLab.Application.Features.Elements.GetElementById;
using ReactionLab.Application.Features.Elements.TranslateElement;

namespace ReactionLab.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddElementHandlers();

        return services;
    }

    private static IServiceCollection AddElementHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetElementByIdHandler>();
        services.AddScoped<TranslateElementHandler>();

        return services;
    }
}

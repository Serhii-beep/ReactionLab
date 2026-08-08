using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ReactionLab.Application.Common.Behaviors;
using System.Reflection;

namespace ReactionLab.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

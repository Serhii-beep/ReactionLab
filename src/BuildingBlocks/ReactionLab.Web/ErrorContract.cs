using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ReactionLab.Web;

public static class ErrorContract
{
    public static IServiceCollection AddErrorContract(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblems.BringIntoContract);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static IApplicationBuilder UseErrorContract(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        return app;
    }
}

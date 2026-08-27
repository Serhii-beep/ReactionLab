using System.Text.Json.Serialization;
using Microsoft.AspNetCore.ResponseCompression;
using ReactionLab.API.Http;

namespace ReactionLab.API;

internal static class DependencyInjection
{
    public const string CorsPolicy = "ReactionLabClient";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblems.BringIntoContract);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:4200"];

            policy.WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }));

        services.AddRateLimiter(RateLimitPolicies.Configure);

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        return services;
    }
}

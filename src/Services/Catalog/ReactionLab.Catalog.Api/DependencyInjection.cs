using System.Text.Json.Serialization;
using Microsoft.AspNetCore.ResponseCompression;
using ReactionLab.Catalog.Api.Http;
using ReactionLab.Web;

namespace ReactionLab.Catalog.Api;

internal static class DependencyInjection
{
    public const string CorsPolicy = "ReactionLabClient";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddErrorContract();
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

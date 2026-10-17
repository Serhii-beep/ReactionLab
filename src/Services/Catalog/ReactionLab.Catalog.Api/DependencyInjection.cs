using System.Text.Json.Serialization;
using Microsoft.AspNetCore.ResponseCompression;
using ReactionLab.Catalog.Api.Http;
using ReactionLab.Web;

namespace ReactionLab.Catalog.Api;

internal static class DependencyInjection
{
    public const string CorsPolicy = "ReactionLabClient";

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddErrorContract();
        services.AddOpenApi();

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

using Microsoft.AspNetCore.ResponseCompression;
using ReactionLab.Catalog.Api.Http;
using ReactionLab.Catalog.Application.Common.Serialization;
using ReactionLab.Web;

namespace ReactionLab.Catalog.Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options => ResponseJson.Configure(options.SerializerOptions));

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

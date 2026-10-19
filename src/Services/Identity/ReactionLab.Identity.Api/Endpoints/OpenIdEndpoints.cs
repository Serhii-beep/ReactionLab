using ReactionLab.Identity.Api.Features.OpenId;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class OpenIdEndpoints
{
    public static IEndpointRouteBuilder MapOpenIdEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethods(
            "/connect/authorize",
            [HttpMethods.Get, HttpMethods.Post],
            (HttpContext httpContext, AuthorizationEndpointHandler handler) => handler.HandleAsync(httpContext))
            .ExcludeFromDescription();

        app.MapPost(
            "/connect/token",
            (HttpContext httpContext, TokenEndpointHandler handler) => handler.HandleAsync(httpContext))
            .ExcludeFromDescription();

        app.MapMethods(
            "/connect/endsession",
            [HttpMethods.Get, HttpMethods.Post],
            (HttpContext httpContext, EndSessionEndpointHandler handler) => handler.HandleAsync(httpContext))
            .ExcludeFromDescription();

        return app;
    }
}

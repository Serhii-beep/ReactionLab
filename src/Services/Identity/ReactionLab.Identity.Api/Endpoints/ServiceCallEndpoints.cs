using Microsoft.AspNetCore.Mvc;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Identity.Api.ServiceCalls;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class ServiceCallEndpoints
{
    private const long MaximumRequestBodyBytesAboveStreamWindow = 1024 * 1024;

    public static IEndpointRouteBuilder MapServiceCallEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGrpcService<ContactsService>()
            .RequireAuthorization(AuthorizationPolicies.ContactsReader)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBodyBytesAboveStreamWindow));

        app.MapGrpcService<SecurityLinksService>()
            .RequireAuthorization(AuthorizationPolicies.SecurityLinksIssuer)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBodyBytesAboveStreamWindow));

        return app;
    }
}

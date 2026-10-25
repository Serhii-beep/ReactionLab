using System.Security.Claims;
using ReactionLab.Identity.Api.Features.Organizations.Contracts;
using ReactionLab.Identity.Api.Features.Organizations.CreateOrganization;
using ReactionLab.Identity.Api.Features.Organizations.GetOrganization;
using ReactionLab.Identity.Api.Features.Organizations.IssueAdminSetupLink;
using ReactionLab.Identity.Api.Features.Organizations.ListOrganizations;
using ReactionLab.Identity.Api.Http;
using ReactionLab.SharedKernel.Pagination;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class OrganizationEndpoints
{
    public static RouteGroupBuilder MapOrganizationEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/organizations")
            .WithTags("Organizations")
            .RequireAuthorization(AuthorizationPolicies.PlatformAdmin)
            .WithNoStore();

        group.MapPost(string.Empty, async (
            CreateOrganizationRequest request,
            ClaimsPrincipal user,
            HttpContext httpContext,
            CreateOrganizationHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateOrganizationCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                request.Kind,
                request.Name,
                request.DistrictId,
                request.FirstAdmin?.DisplayName,
                request.FirstAdmin?.Username,
                request.FirstAdmin?.Email,
                request.FirstAdmin?.Locale ?? httpContext.ResolveLocale().Code);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToCreatedResult(created => $"/api/v1/organizations/{created.Organization.Id}");
        })
        .WithName("CreateOrganization")
        .WithSummary("Create a district or school. For platform admins who confirmed their password in this session.")
        .Produces<CreatedOrganizationResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, async (
            Guid? districtId,
            [AsParameters] CursorRequest page,
            ListOrganizationsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListOrganizationsQuery(districtId, page);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListOrganizations")
        .WithSummary("List the organizations, or a district's schools, oldest first. For platform admins.")
        .Produces<CursorPagedResult<OrganizationResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{organizationId:guid}", async (
            Guid organizationId,
            GetOrganizationHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationQuery(organizationId);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GetOrganization")
        .WithSummary("Read an organization. For platform admins.")
        .Produces<OrganizationResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{organizationId:guid}/admins/{accountId:guid}/setup-links", async (
            Guid organizationId,
            Guid accountId,
            ClaimsPrincipal user,
            IssueAdminSetupLinkHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new IssueAdminSetupLinkCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                accountId);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("IssueAdminSetupLink")
        .WithSummary("Replace an admin's setup link. For platform admins who confirmed their password in this session.")
        .Produces<AccountSetupLinkResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }
}

using System.Security.Claims;
using ReactionLab.Identity.Api.Features.Members.Contracts;
using ReactionLab.Identity.Api.Features.Members.CreateMember;
using ReactionLab.Identity.Api.Features.Members.DeleteMember;
using ReactionLab.Identity.Api.Features.Members.DisableMember;
using ReactionLab.Identity.Api.Features.Members.EnableMember;
using ReactionLab.Identity.Api.Features.Members.EndMemberSessions;
using ReactionLab.Identity.Api.Features.Members.GetMember;
using ReactionLab.Identity.Api.Features.Members.GrantMemberRole;
using ReactionLab.Identity.Api.Features.Members.ImportMembers;
using ReactionLab.Identity.Api.Features.Members.ListMembers;
using ReactionLab.Identity.Api.Features.Members.RenameMember;
using ReactionLab.Identity.Api.Features.Members.ResetMemberPassword;
using ReactionLab.Identity.Api.Features.Members.RevokeMemberRole;
using ReactionLab.Identity.Api.Features.Members.TransferMember;
using ReactionLab.Identity.Api.Features.Organizations.Contracts;
using ReactionLab.Identity.Api.Features.Organizations.GetOrganizationSettings;
using ReactionLab.Identity.Api.Features.Organizations.ListOrganizationChanges;
using ReactionLab.Identity.Api.Features.Organizations.UpdateOrganizationSettings;
using ReactionLab.Identity.Api.Http;
using ReactionLab.SharedKernel.Pagination;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class SchoolAdministrationEndpoints
{
    public static RouteGroupBuilder MapSchoolAdministrationEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/organizations/{organizationId:guid}")
            .WithTags("School administration")
            .RequireAuthorization(AuthorizationPolicies.AccessToken)
            .WithNoStore();

        group.MapGet("/members", async (
            Guid organizationId,
            string? search,
            string? role,
            string? status,
            [AsParameters] CursorRequest page,
            ClaimsPrincipal user,
            ListMembersHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListMembersQuery(user.GetAccountId(), organizationId, search, role, status, page);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListMembers")
        .WithSummary("List and search an organization's members, oldest first. Its teachers see its students.")
        .Produces<CursorPagedResult<MemberResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/members/{accountId:guid}", async (
            Guid organizationId,
            Guid accountId,
            ClaimsPrincipal user,
            GetMemberHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetMemberQuery(user.GetAccountId(), organizationId, accountId);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GetMember")
        .WithSummary("Read a member of an organization.")
        .Produces<MemberResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/members", async (
            Guid organizationId,
            CreateMemberRequest request,
            ClaimsPrincipal user,
            CreateMemberHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateMemberCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                request.DisplayName,
                request.Username,
                request.Email,
                request.Locale,
                request.Role,
                request.SchoolId,
                request.Credential);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToCreatedResult(created =>
                $"/api/v1/organizations/{organizationId}/members/{created.Member.AccountId}");
        })
        .WithName("CreateMember")
        .WithSummary("Create a school account, with a password shown once or a setup link. For admins.")
        .RequireRateLimiting(RateLimitPolicies.MemberCredentials)
        .Produces<CreatedMemberResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/member-imports", async (
            Guid organizationId,
            ImportMembersRequest request,
            ClaimsPrincipal user,
            ImportMembersHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ImportMembersCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                request.Csv,
                request.Credential);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ImportMembers")
        .WithSummary("Create up to 100 school accounts from a CSV, all or none. For a school's admins.")
        .RequireRateLimiting(RateLimitPolicies.MemberImports)
        .Produces<ImportedMembersResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPatch("/members/{accountId:guid}", async (
            Guid organizationId,
            Guid accountId,
            RenameMemberRequest request,
            ClaimsPrincipal user,
            RenameMemberHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RenameMemberCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                accountId,
                request.DisplayName,
                request.Username);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RenameMember")
        .WithSummary("Change a member's display name or username. For admins.")
        .Produces<MemberResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/members/{accountId:guid}/roles/{role}", async (
            Guid organizationId,
            Guid accountId,
            string role,
            ClaimsPrincipal user,
            GrantMemberRoleHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GrantMemberRoleCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                accountId,
                role);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GrantMemberRole")
        .WithSummary("Give a member a role here. For admins.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/members/{accountId:guid}/roles/{role}", async (
            Guid organizationId,
            Guid accountId,
            string role,
            ClaimsPrincipal user,
            RevokeMemberRoleHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RevokeMemberRoleCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                accountId,
                role);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RevokeMemberRole")
        .WithSummary("Take a role here from a member. For admins.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/members/{accountId:guid}/password-resets", async (
            Guid organizationId,
            Guid accountId,
            ResetMemberPasswordRequest request,
            ClaimsPrincipal user,
            ResetMemberPasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ResetMemberPasswordCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                accountId,
                request.Credential);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ResetMemberPassword")
        .WithSummary("Give a member a new password shown once, or a setup link, ending every session.")
        .RequireRateLimiting(RateLimitPolicies.MemberCredentials)
        .Produces<MemberCredentialResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPut("/members/{accountId:guid}/disabled", async (
            Guid organizationId,
            Guid accountId,
            ClaimsPrincipal user,
            DisableMemberHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DisableMemberCommand(user.GetAccountId(), user.GetSessionId(), organizationId, accountId);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("DisableMember")
        .WithSummary("Disable a member's account, ending every session. For admins.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/members/{accountId:guid}/disabled", async (
            Guid organizationId,
            Guid accountId,
            ClaimsPrincipal user,
            EnableMemberHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new EnableMemberCommand(user.GetAccountId(), user.GetSessionId(), organizationId, accountId);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("EnableMember")
        .WithSummary("Enable a disabled member's account again. For admins.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/members/{accountId:guid}/session-revocations", async (
            Guid organizationId,
            Guid accountId,
            ClaimsPrincipal user,
            EndMemberSessionsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new EndMemberSessionsCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                accountId);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("EndMemberSessions")
        .WithSummary("Sign a member out everywhere. For admins.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/members/{accountId:guid}", async (
            Guid organizationId,
            Guid accountId,
            ClaimsPrincipal user,
            DeleteMemberHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteMemberCommand(user.GetAccountId(), user.GetSessionId(), organizationId, accountId);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("DeleteMember")
        .WithSummary("Delete a member's account. For the admins of the organization it belongs to.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/members/{accountId:guid}/transfers", async (
            Guid organizationId,
            Guid accountId,
            TransferMemberRequest request,
            ClaimsPrincipal user,
            TransferMemberHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new TransferMemberCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                accountId,
                request.OrganizationId);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("TransferMember")
        .WithSummary("Move a student to another school of the district, ending every session. For district admins.")
        .Produces<MemberResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/settings", async (
            Guid organizationId,
            ClaimsPrincipal user,
            GetOrganizationSettingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationSettingsQuery(user.GetAccountId(), organizationId);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GetOrganizationSettings")
        .WithSummary("Read an organization's settings. For its admins.")
        .Produces<OrganizationSettingsResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/settings", async (
            Guid organizationId,
            UpdateOrganizationSettingsRequest request,
            ClaimsPrincipal user,
            UpdateOrganizationSettingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateOrganizationSettingsCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                organizationId,
                request.Name,
                request.Locale,
                request.TimeZone,
                request.SignInCode,
                request.TeachersMayResetStudentPasswords);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("UpdateOrganizationSettings")
        .WithSummary("Change an organization's name, language, time zone, sign-in code or teachers' resets.")
        .Produces<OrganizationSettingsResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/changes", async (
            Guid organizationId,
            [AsParameters] CursorRequest page,
            ClaimsPrincipal user,
            ListOrganizationChangesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListOrganizationChangesQuery(user.GetAccountId(), organizationId, page);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListOrganizationChanges")
        .WithSummary("Read an organization's record, newest first. For its admins.")
        .Produces<CursorPagedResult<OrganizationChangeResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}

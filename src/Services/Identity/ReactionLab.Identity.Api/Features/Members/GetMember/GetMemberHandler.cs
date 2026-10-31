using ReactionLab.Identity.Api.Features.Members.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.GetMember;

internal sealed class GetMemberHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider) : IQueryHandler<GetMemberQuery, MemberResponse>
{
    public async ValueTask<Result<MemberResponse>> HandleAsync(
        GetMemberQuery query,
        CancellationToken cancellationToken)
    {
        var access = await adminTransaction.AuthorizeAsync(
            query.ActorId,
            query.OrganizationId,
            SchoolAdminRules.AdminOrTeacher,
            cancellationToken);

        if (access.IsFailure)
        {
            return Result.Failure<MemberResponse>(access.Error);
        }

        var now = timeProvider.GetUtcNow();
        var member = MemberTargets.Visible(
            await context.FindMemberAsync(
                query.OrganizationId,
                access.Value.DistrictId,
                query.AccountId,
                now,
                cancellationToken),
            query.OrganizationId);

        if (member.IsFailure || (!access.Value.IsAdmin && !member.Value.Roles.Contains(SchoolRoleNames.Student)))
        {
            return Result.Failure<MemberResponse>(MemberErrors.NotFound);
        }

        var response = MemberResponse.From(member.Value, now);

        return Result.Success(access.Value.IsAdmin ? response : response with { Email = null });
    }
}

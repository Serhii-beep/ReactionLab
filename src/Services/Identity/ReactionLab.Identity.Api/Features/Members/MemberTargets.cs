using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members;

internal static class MemberTargets
{
    public static Result<SchoolMember> Visible(SchoolMember? member, Guid organizationId)
    {
        if (member is null || (member.OrganizationId != organizationId && member.Roles.Count == 0))
        {
            return MemberErrors.NotFound;
        }

        return member;
    }

    public static Result<SchoolMember> Managed(SchoolMember? member, Guid organizationId)
    {
        var visible = Visible(member, organizationId);

        if (visible.IsFailure)
        {
            return visible;
        }

        return visible.Value.OrganizationId == organizationId ? visible : MemberErrors.ManagedByDistrict;
    }
}

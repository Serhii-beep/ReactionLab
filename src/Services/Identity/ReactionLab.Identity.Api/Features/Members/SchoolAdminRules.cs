using ReactionLab.Identity.Api.Features.Organizations;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members;

internal static class SchoolAdminRules
{
    public static Error? Admin(OrganizationAccess access) =>
        !access.IsAdmin ? OrganizationErrors.AdminRequired
        : access.IsSuspended ? OrganizationErrors.Suspended
        : null;

    public static Error? DistrictAdmin(OrganizationAccess access) =>
        !access.IsDistrictAdmin ? OrganizationErrors.DistrictAdminRequired
        : access.IsSuspended ? OrganizationErrors.Suspended
        : null;

    public static Error? AdminOrTeacher(OrganizationAccess access) =>
        !access.IsAdmin && !access.IsTeacher ? OrganizationErrors.AdminRequired
        : access.IsSuspended ? OrganizationErrors.Suspended
        : null;

    public static Error? StudentPasswordReset(OrganizationAccess access) =>
        !access.IsAdmin && !access.IsTeacher ? OrganizationErrors.AdminRequired
        : access.IsSuspended ? OrganizationErrors.Suspended
        : access.IsAdmin || access.TeachersMayResetStudentPasswords ? null
        : MemberErrors.ResetNotAllowed;
}

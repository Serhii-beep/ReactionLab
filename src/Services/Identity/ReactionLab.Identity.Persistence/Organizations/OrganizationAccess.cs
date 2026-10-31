namespace ReactionLab.Identity.Persistence.Organizations;

public sealed record OrganizationAccess(
    Guid OrganizationId,
    string Kind,
    Guid? DistrictId,
    bool IsSuspended,
    bool IsAdmin,
    bool IsDistrictAdmin,
    bool IsTeacher,
    bool TeachersMayResetStudentPasswords,
    string Locale);

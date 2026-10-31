using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members;

internal static class MemberErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Member.NotFound",
        "The organization has no member with this id.");

    public static readonly Error ManagedByDistrict = Error.Forbidden(
        "Member.ManagedByDistrict",
        "The district's admins manage this account.");

    public static readonly Error RoleUnknown = Error.Validation(
        "Member.RoleUnknown",
        "A role is school-admin, teacher or student at a school, and district-admin at a district.",
        "Role");

    public static readonly Error StudentRoleExclusive = Error.Conflict(
        "Member.StudentRoleExclusive",
        "A student holds no other role at the same school.");

    public static readonly Error StudentAtOwnSchoolOnly = Error.Conflict(
        "Member.StudentAtOwnSchoolOnly",
        "Only a school's own accounts are its students.");

    public static readonly Error LastAdmin = Error.Conflict(
        "Member.LastAdmin",
        "The organization keeps at least one admin whose account is enabled.");

    public static readonly Error RolesChanged = Error.Conflict(
        "Member.RolesChanged",
        "The account's roles changed during the request. Try again.");

    public static readonly Error UsernameInUse = Error.Conflict(
        "Member.UsernameInUse",
        "Another account here has this username.",
        "Username");

    public static readonly Error EmailInUse = Error.Conflict(
        "Member.EmailInUse",
        "Another account here has this email address.",
        "Email");

    public static readonly Error CredentialUnknown = Error.Validation(
        "Member.CredentialUnknown",
        "A credential is password or setup-link.",
        "Credential");

    public static readonly Error SchoolNotFound = Error.NotFound(
        "Member.SchoolNotFound",
        "The district has no school with this id.",
        "SchoolId");

    public static readonly Error NothingToChange = Error.Validation(
        "Member.NothingToChange",
        "Give a new display name, a new username, or both.");

    public static readonly Error ResetNotAllowed = Error.Forbidden(
        "Member.ResetNotAllowed",
        "Teachers here may reset students' passwords only, and only when the school allows it.");

    public static readonly Error NotTransferable = Error.Conflict(
        "Member.NotTransferable",
        "Only a student moves, and only to another school of the same district.");

    public static readonly Error DestinationRequired = Error.Validation(
        "Member.DestinationRequired",
        "Name the school the student moves to.",
        "OrganizationId");

    public static readonly Error DestinationNotFound = Error.NotFound(
        "Member.DestinationNotFound",
        "The district has no school with this id.",
        "OrganizationId");

    public static readonly Error SearchTooLong = Error.Validation(
        "Member.SearchTooLong",
        $"A search must not exceed {MemberSearch.MaximumTextLength} characters.",
        "Search")
        .WithArgs(("max", MemberSearch.MaximumTextLength));

    public static readonly Error SearchInvalid = Error.Validation(
        "Member.SearchInvalid",
        "A search must not contain control characters.",
        "Search");

    public static readonly Error StatusUnknown = Error.Validation(
        "Member.StatusUnknown",
        "A status is active, pending, locked or disabled.",
        "Status");
}

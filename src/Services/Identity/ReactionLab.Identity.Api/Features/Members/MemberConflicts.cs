using Npgsql;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members;

internal static class MemberConflicts
{
    private const string UsernameIndex = "ix_users_organization_id_organization_username";
    private const string EmailIndex = "ix_users_organization_id_normalized_email";

    public static Error? Of(Exception exception) =>
        (exception as PostgresException ?? exception.InnerException as PostgresException) switch
        {
            { SqlState: PostgresErrorCodes.UniqueViolation } violation => violation.ConstraintName switch
            {
                UsernameIndex => MemberErrors.UsernameInUse,
                EmailIndex => MemberErrors.EmailInUse,
                _ => null
            },
            _ => null
        };
}

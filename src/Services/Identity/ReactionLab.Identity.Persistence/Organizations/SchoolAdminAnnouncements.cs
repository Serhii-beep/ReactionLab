using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Persistence.Organizations;

public static class SchoolAdminAnnouncements
{
    public static bool IsAdminRole(string? role) =>
        role is SchoolRoleNames.SchoolAdmin or SchoolRoleNames.DistrictAdmin;

    public static async Task AnnounceSchoolAdminChangeAsync(
        this AppDbContext context,
        OutboxWriter outbox,
        Guid organizationId,
        Guid changeId,
        IReadOnlyCollection<Guid> changedAccountIds)
    {
        var recipients = await context.ListOrganizationAdminRecipientsAsync(
            organizationId,
            changedAccountIds,
            CancellationToken.None);

        await outbox.InsertAllAsync(
            context,
            recipients.Select(recipient =>
                new SchoolAdminChangeAnnounced(recipient.AccountId, recipient.Version, changeId)));
    }
}

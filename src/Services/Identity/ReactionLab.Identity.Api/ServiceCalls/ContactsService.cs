using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.AccountSetups;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.Identity.Persistence.PlatformRoles;

namespace ReactionLab.Identity.Api.ServiceCalls;

internal sealed class ContactsService(
    AppDbContext identityDatabase,
    ExternalSignInProviders providers) : Contacts.ContactsBase
{
    public override async Task<Contact> GetContact(GetContactRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The account id is not a GUID."));
        }

        var contact = await identityDatabase.FindContactAsync(accountId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "The account doesn't exist."));

        var reply = new Contact
        {
            AccountId = accountId.ToString(),
            EmailConfirmed = contact.EmailConfirmed,
            DisplayName = contact.DisplayName,
            Locale = contact.PreferredLocale,
            TimeZone = contact.TimeZone ?? string.Empty
        };

        if (contact.Email is { } email)
        {
            reply.Email = email;
        }

        return reply;
    }

    public override async Task<PasswordChangeDetails> GetPasswordChange(
        GetPasswordChangeRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId) || !Guid.TryParse(request.ChangeId, out var changeId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The account and change ids are GUIDs."));
        }

        var change = await identityDatabase.FindPasswordChangeAsync(accountId, changeId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "The change is gone or never happened."));

        var device = DeviceDescriptions.FromUserAgent(change.UserAgent);

        return new PasswordChangeDetails
        {
            ChangedAt = Timestamp.FromDateTimeOffset(change.ChangedAt),
            Browser = device.Browser,
            OperatingSystem = device.OperatingSystem
        };
    }

    public override async Task<SignInMethodChangeDetails> GetSignInMethodChange(
        GetSignInMethodChangeRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId) || !Guid.TryParse(request.ChangeId, out var changeId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The account and change ids are GUIDs."));
        }

        var change = await identityDatabase.FindSignInMethodChangeAsync(accountId, changeId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "The change is gone or never happened."));
        var methods = await identityDatabase.FindSignInMethodsAsync(accountId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "The account doesn't exist."));
        var device = DeviceDescriptions.FromUserAgent(change.UserAgent);

        var reply = new SignInMethodChangeDetails
        {
            ChangedAt = Timestamp.FromDateTimeOffset(change.ChangedAt),
            Change = SignInMethodChangeOf(change.Change),
            ProviderDisplayName = change.Provider is { } provider ? ProviderDisplayName(provider) : string.Empty,
            Browser = device.Browser,
            OperatingSystem = device.OperatingSystem,
            HasPassword = methods.HasPassword
        };

        reply.ProviderDisplayNames.AddRange(methods.Logins.Select(login => ProviderDisplayName(login.Provider)));

        return reply;
    }

    public override async Task<LockoutDetails> GetLockout(GetLockoutRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The account id is not a GUID."));
        }

        var lockout = await identityDatabase.FindAccountLockoutAsync(accountId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "The account doesn't exist."));

        var reply = new LockoutDetails
        {
            SignInDisabled = lockout.LockoutEnabled && lockout.SignInLockoutEnd == DateTimeOffset.MaxValue,
            SignInFailures = lockout.SignInFailures,
            PasswordChecksDisabled = lockout.PasswordCheckLockoutEnd == DateTimeOffset.MaxValue,
            PasswordCheckFailures = lockout.PasswordCheckFailures
        };

        if (lockout is { LockoutEnabled: true, SignInLockoutEnd: { } signInLockoutEnd }
            && signInLockoutEnd != DateTimeOffset.MaxValue)
        {
            reply.SignInPausedUntil = Timestamp.FromDateTimeOffset(signInLockoutEnd);
        }

        if (lockout.PasswordCheckLockoutEnd is { } passwordCheckLockoutEnd
            && passwordCheckLockoutEnd != DateTimeOffset.MaxValue)
        {
            reply.PasswordChecksPausedUntil = Timestamp.FromDateTimeOffset(passwordCheckLockoutEnd);
        }

        return reply;
    }

    public override async Task<PlatformRoleChangeDetails> GetPlatformRoleChange(
        GetPlatformRoleChangeRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId) || !Guid.TryParse(request.ChangeId, out var changeId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The account and change ids are GUIDs."));
        }

        var change = await identityDatabase.FindPlatformRoleChangeAsync(accountId, changeId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "The change is gone or isn't the account's."));

        return new PlatformRoleChangeDetails
        {
            ChangedAt = Timestamp.FromDateTimeOffset(change.ChangedAt),
            Role = change.Role,
            Granted = change.Granted,
            Source = change.Source switch
            {
                PlatformRoleChangeAuthor.AdminSource => PlatformRoleChangeSource.Admin,
                PlatformRoleChangeAuthor.FirstAdminSource => PlatformRoleChangeSource.FirstAdmin,
                PlatformRoleChangeAuthor.BreakGlassSource => PlatformRoleChangeSource.BreakGlass,
                _ => PlatformRoleChangeSource.Unspecified
            },
            AccountDisplayName = change.AccountDisplayName ?? string.Empty,
            AccountEmail = change.RecipientIsAdmin ? change.AccountEmail ?? string.Empty : string.Empty,
            ChangedByDisplayName = change.ChangedByDisplayName ?? string.Empty,
            ChangedByEmail = change.RecipientIsAdmin ? change.ChangedByEmail ?? string.Empty : string.Empty
        };
    }

    public override async Task<OrganizationChangeDetails> GetOrganizationChange(
        GetOrganizationChangeRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId) || !Guid.TryParse(request.ChangeId, out var changeId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The account and change ids are GUIDs."));
        }

        var change = await identityDatabase.FindOrganizationChangeAsync(accountId, changeId, context.CancellationToken);
        var adminRoleChange = change?.Change switch
        {
            OrganizationChangeNames.RoleGranted or OrganizationChangeNames.AccountCreated => AdminRoleChange.Granted,
            OrganizationChangeNames.RoleRevoked => AdminRoleChange.Revoked,
            OrganizationChangeNames.AccountDeleted => AdminRoleChange.AccountDeleted,
            _ => AdminRoleChange.Unspecified
        };

        if (change is null || adminRoleChange == AdminRoleChange.Unspecified)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "The change is gone or isn't an admin change."));
        }

        return new OrganizationChangeDetails
        {
            ChangedAt = Timestamp.FromDateTimeOffset(change.ChangedAt),
            OrganizationName = change.OrganizationName,
            Change = adminRoleChange,
            Role = change.Role ?? SchoolRoleNames.AdminOf(change.OrganizationKind),
            AccountDisplayName = change.AccountDisplayName ?? string.Empty,
            AccountUsername = change.AccountUsername ?? string.Empty,
            ChangedByDisplayName = change.ChangedByPlatform
                ? string.Empty
                : change.ChangedByDisplayName ?? string.Empty,
            ChangedByPlatform = change.ChangedByPlatform,
            AccountCount = change.AccountCount
        };
    }

    public override async Task<AccountSetupDetails> GetAccountSetup(
        GetAccountSetupRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId)
            || !Guid.TryParse(request.RequestId, out var requestId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The account and request ids are GUIDs."));
        }

        var setup = await identityDatabase.FindAccountSetupDescriptionAsync(
                accountId,
                requestId,
                context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "The setup was replaced, used or expired."));

        var reply = new AccountSetupDetails
        {
            Purpose = setup.Purpose switch
            {
                AccountSetupPurposes.Invitation => AccountSetupPurpose.Invitation,
                AccountSetupPurposes.AdminReset => AccountSetupPurpose.AdminReset,
                AccountSetupPurposes.SelfReset => AccountSetupPurpose.SelfReset,
                _ => AccountSetupPurpose.Unspecified
            },
            OrganizationName = setup.OrganizationName,
            SignInCode = setup.SignInCode,
            Username = setup.Username ?? string.Empty,
            RequestedByDisplayName = setup.RequestedByDisplayName ?? string.Empty,
            RequestedByPlatformAdmin = setup.RequestedByPlatformAdmin
        };

        reply.Roles.AddRange(setup.Roles.Order(StringComparer.Ordinal));
        reply.RequestedByRoles.AddRange(setup.RequestedByRoles.Order(StringComparer.Ordinal));

        return reply;
    }

    private static SignInMethodChangeKind SignInMethodChangeOf(string change) =>
        change switch
        {
            SignInMethodChangeNames.ProviderConnected => SignInMethodChangeKind.ProviderConnected,
            SignInMethodChangeNames.ProviderDisconnected => SignInMethodChangeKind.ProviderDisconnected,
            SignInMethodChangeNames.PasswordSet => SignInMethodChangeKind.PasswordSet,
            SignInMethodChangeNames.PasswordRemoved => SignInMethodChangeKind.PasswordRemoved,
            SignInMethodChangeNames.AccountTakenOver => SignInMethodChangeKind.AccountTakenOver,
            _ => SignInMethodChangeKind.Unspecified
        };

    private string ProviderDisplayName(string provider) => providers.Find(provider)?.DisplayName ?? provider;
}

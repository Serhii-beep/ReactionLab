using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Client.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.Accounts.ChangePassword;
using ReactionLab.Identity.Api.Features.Accounts.GetProfile;
using ReactionLab.Identity.Api.Features.Accounts.ListAccountOrganizations;
using ReactionLab.Identity.Api.Features.Accounts.Reauthenticate;
using ReactionLab.Identity.Api.Features.Accounts.RegisterAccount;
using ReactionLab.Identity.Api.Features.Accounts.RemovePassword;
using ReactionLab.Identity.Api.Features.Accounts.RequestEmailConfirmation;
using ReactionLab.Identity.Api.Features.Accounts.RequestPasswordReset;
using ReactionLab.Identity.Api.Features.Accounts.ResetPassword;
using ReactionLab.Identity.Api.Features.Accounts.SchoolSignIn;
using ReactionLab.Identity.Api.Features.Accounts.SetPassword;
using ReactionLab.Identity.Api.Features.Accounts.SignIn;
using ReactionLab.Identity.Api.Features.Accounts.SignOut;
using ReactionLab.Identity.Api.Features.Accounts.SwitchActiveOrganization;
using ReactionLab.Identity.Api.Features.Accounts.UpdateProfile;
using ReactionLab.Identity.Api.Features.AccountSetups.CompleteAccountSetup;
using ReactionLab.Identity.Api.Features.AccountSetups.LookUpAccountSetup;
using ReactionLab.Identity.Api.Features.ExternalSignIn;
using ReactionLab.Identity.Api.Features.ExternalSignIn.CompleteExternalSignIn;
using ReactionLab.Identity.Api.Features.ExternalSignIn.GetExternalIdentity;
using ReactionLab.Identity.Api.Features.ExternalSignIn.LinkExternalSignIn;
using ReactionLab.Identity.Api.Features.ExternalSignIn.ListSignInMethods;
using ReactionLab.Identity.Api.Features.ExternalSignIn.RegisterExternalAccount;
using ReactionLab.Identity.Api.Features.ExternalSignIn.StartExternalSignIn;
using ReactionLab.Identity.Api.Features.ExternalSignIn.TakeOverAccount;
using ReactionLab.Identity.Api.Features.Logins.CompleteLoginConnection;
using ReactionLab.Identity.Api.Features.Logins.ConnectLogin;
using ReactionLab.Identity.Api.Features.Logins.DisconnectLogin;
using ReactionLab.Identity.Api.Features.Logins.ListLogins;
using ReactionLab.Identity.Api.Features.Members;
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
using ReactionLab.Identity.Api.Features.OpenId;
using ReactionLab.Identity.Api.Features.Organizations.CreateOrganization;
using ReactionLab.Identity.Api.Features.Organizations.GetOrganization;
using ReactionLab.Identity.Api.Features.Organizations.GetOrganizationSettings;
using ReactionLab.Identity.Api.Features.Organizations.IssueAdminSetupLink;
using ReactionLab.Identity.Api.Features.Organizations.ListOrganizationChanges;
using ReactionLab.Identity.Api.Features.Organizations.ListOrganizations;
using ReactionLab.Identity.Api.Features.Organizations.ReinstateOrganization;
using ReactionLab.Identity.Api.Features.Organizations.SuspendOrganization;
using ReactionLab.Identity.Api.Features.Organizations.UpdateOrganizationSettings;
using ReactionLab.Identity.Api.Features.PlatformRoles;
using ReactionLab.Identity.Api.Features.PlatformRoles.GrantPlatformRole;
using ReactionLab.Identity.Api.Features.PlatformRoles.ListPlatformRoleAccounts;
using ReactionLab.Identity.Api.Features.PlatformRoles.ListPlatformRoleChanges;
using ReactionLab.Identity.Api.Features.PlatformRoles.LookUpPlatformRoleAccount;
using ReactionLab.Identity.Api.Features.PlatformRoles.RevokePlatformRole;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Api.Features.Sessions.ListSessions;
using ReactionLab.Identity.Api.Features.Sessions.RevokeOtherSessions;
using ReactionLab.Identity.Api.Features.Sessions.RevokeSession;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Api.ServiceCalls;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.SharedKernel;
using ReactionLab.Web;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api;

internal static class DependencyInjection
{
    private const string IdentityMeterName = "Microsoft.AspNetCore.Identity";
    private const string IdentityCookieName = "__Host-identity";
    private const string PendingExternalSignInCookieName = "__Host-identity-external";
    private const string CorrelationCookieName = "__Host-identity-correlation";
    private const string IssuerKey = "OpenId:Issuer";
    private const int MaximumServiceCallMessageBytes = 64 * 1024;

    private static readonly TimeSpan IdentityCookieLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ExternalSignInStateLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PendingExternalSignInLifetime = TimeSpan.FromMinutes(10);

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddErrorContract();
        services.AddOpenApi();

        services.AddRateLimiter(RateLimitPolicies.Configure);
        services.AddAuthorization(AuthorizationPolicies.Configure);
        services.AddGrpc(ConfigureServiceCalls);

        return services;
    }

    public static IServiceCollection AddAccounts(this IServiceCollection services)
    {
        EmailAddresses.RequireIdnaSupport();
        IanaTimeZones.RequireSupport();

        services.TryAddSingleton(TimeProvider.System);

        services.AddIdentityCore<User>(ConfigureIdentity)
            .AddUserStore<RegularAccountUserStore>()
            .AddSignInManager();

        services.RemoveAll<IPasswordValidator<User>>();
        services.AddScoped<IPasswordValidator<User>, PasswordRulesValidator>();

        services.Configure<PasswordHasherOptions>(options => options.IterationCount = PasswordPolicy.HashIterations);
        services.Replace(ServiceDescriptor.Singleton<IPasswordHasher<User>, NormalizingPasswordHasher>());
        services.Replace(ServiceDescriptor.Scoped<IUserConfirmation<User>, AccountConfirmation>());
        services.AddSingleton<PasswordVerificationGate>();

        services.AddHttpClient<PwnedPasswordsClient>(PwnedPasswordsClient.Configure).RemoveAllLoggers();
        services.Configure<HttpClientTraceInstrumentationOptions>(ExcludePwnedPasswordsFromTraces);

        services.AddAuthentication().AddIdentityCookies();
        services.ConfigureApplicationCookie(ConfigureIdentityCookie);

        services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(IdentityMeterName, SignInMetrics.MeterName));

        services.AddSingleton<SignInMetrics>();
        services.AddScoped<SignInAttempts>();
        services.AddScoped<PasswordSignInVerifier>();
        services.AddScoped<RegisterAccountHandler>();
        services.AddScoped<RequestEmailConfirmationHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<PasswordChanges>();
        services.AddHostedService<AccountRetentionWorker>();
        services.AddScoped<SignInHandler>();
        services.AddScoped<SchoolSignInHandler>();
        services.AddScoped<SignOutHandler>();
        services.AddScoped<GetProfileHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<CurrentPasswordCheck>();
        services.AddScoped<NewPasswordCheck>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<ReauthenticateHandler>();
        services.AddScoped<SetPasswordHandler>();
        services.AddScoped<RemovePasswordHandler>();

        return services;
    }

    public static IServiceCollection AddOpenIdServer(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var issuer = ReadIssuer(configuration);

        services.AddOpenIddict()
            .AddServer(server =>
            {
                server.SetIssuer(issuer)
                    .SetAuthorizationEndpointUris("connect/authorize")
                    .SetPushedAuthorizationEndpointUris("connect/par")
                    .SetTokenEndpointUris("connect/token")
                    .SetEndSessionEndpointUris("connect/endsession")
                    .SetRevocationEndpointUris("connect/revocation");

                server.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow()
                    .RequireProofKeyForCodeExchange()
                    .RequirePushedAuthorizationRequests()
                    .RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.OfflineAccess, OpenIdNames.ApiScope);

                server.SetAccessTokenLifetime(SessionPolicy.AccessTokenLifetime)
                    .SetRefreshTokenLifetime(SessionPolicy.IdleTimeout)
                    .DisableAccessTokenEncryption()
                    .Configure(options =>
                    {
                        options.CodeChallengeMethods.Remove(CodeChallengeMethods.Plain);
                        options.RequestTokenLifetime = SessionPolicy.PushedRequestLifetime;
                    });

                AddCredentials(server, environment);

                server.AddEventHandler(SkipSelfContainedTokenEntries.Descriptor)
                    .AddEventHandler(RevokeSessionWithRefreshToken.Descriptor)
                    .AddEventHandler(RevokeSessionOnTokenReplay.Descriptor);

                server.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            })
            .AddValidation(ConfigureAccessTokenValidation);

        services.AddScoped<SessionClaims>();
        services.AddScoped<SessionRevocation>();
        services.AddScoped<SecurityStampRotation>();
        services.AddScoped<AuthorizationEndpointHandler>();
        services.AddScoped<TokenEndpointHandler>();
        services.AddScoped<EndSessionEndpointHandler>();
        services.AddScoped<RecentSignInCheck>();
        services.AddScoped<ListSessionsHandler>();
        services.AddScoped<RevokeSessionHandler>();
        services.AddScoped<RevokeOtherSessionsHandler>();
        services.AddHostedService<TokenPruningWorker>();

        return services;
    }

    public static IServiceCollection AddExternalSignIn(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var providers = ExternalSignInProviders.FromConfiguration(configuration);

        services.AddSingleton(providers);
        services.AddSingleton<ProviderDiscovery>();
        services.ConfigureExternalCookie(ConfigurePendingExternalSignInCookie);

        services.AddScoped<PendingExternalSignIns>();
        services.AddScoped<ExternalSignInAddresses>();
        services.AddScoped<ListSignInMethodsHandler>();
        services.AddScoped<StartExternalSignInHandler>();
        services.AddScoped<CompleteExternalSignInHandler>();
        services.AddScoped<GetExternalIdentityHandler>();
        services.AddScoped<RegisterExternalAccountHandler>();
        services.AddScoped<LinkExternalSignInHandler>();
        services.AddScoped<TakeOverAccountHandler>();
        services.AddScoped<ListLoginsHandler>();
        services.AddScoped<ConnectLoginHandler>();
        services.AddScoped<CompleteLoginConnectionHandler>();
        services.AddScoped<DisconnectLoginHandler>();

        if (providers.All.Count == 0)
        {
            return services;
        }

        var issuer = ReadIssuer(configuration);

        services.AddOpenIddict()
            .AddClient(client =>
            {
                client.AllowAuthorizationCodeFlow()
                    .DisableTokenStorage()
                    .SetStateTokenLifetime(ExternalSignInStateLifetime)
                    .SetClientUri(issuer);

                AddStateTokenCredentials(client, environment);

                client.AddEventHandler(SkipIssuerCheckOnErrorResponse.Descriptor);

                client.UseAspNetCore()
                    .EnableRedirectionEndpointPassthrough()
                    .EnableErrorPassthrough()
                    .Configure(ConfigureCorrelationCookie);

                client.UseSystemNetHttp()
                    .Configure(options => options.HttpResiliencePipeline = null);

                client.SetRedirectionEndpointUris([.. providers.All.Select(ExternalSignInCallbackPath)]);

                foreach (var provider in providers.All)
                {
                    provider.Register(client, new Uri(issuer, ExternalSignInCallbackPath(provider)));
                }
            });

        services.AddHostedService<ProviderDiscoveryWorker>();

        return services;
    }

    public static IServiceCollection AddPlatformRoles(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PlatformRoleRequirementHandler>();
        services.AddScoped<PlatformAdminTransaction>();
        services.AddScoped<ListPlatformRoleAccountsHandler>();
        services.AddScoped<LookUpPlatformRoleAccountHandler>();
        services.AddScoped<GrantPlatformRoleHandler>();
        services.AddScoped<RevokePlatformRoleHandler>();
        services.AddScoped<ListPlatformRoleChangesHandler>();

        return services;
    }

    public static IServiceCollection AddOrganizations(this IServiceCollection services)
    {
        services.AddScoped<CreateOrganizationHandler>();
        services.AddScoped<ListOrganizationsHandler>();
        services.AddScoped<GetOrganizationHandler>();
        services.AddScoped<IssueAdminSetupLinkHandler>();
        services.AddScoped<SuspendOrganizationHandler>();
        services.AddScoped<ReinstateOrganizationHandler>();
        services.AddScoped<LookUpAccountSetupHandler>();
        services.AddScoped<CompleteAccountSetupHandler>();

        return services;
    }

    public static IServiceCollection AddSchoolAdministration(this IServiceCollection services)
    {
        services.AddScoped<SchoolAdminTransaction>();
        services.AddScoped<MemberCredentials>();
        services.AddScoped<ListMembersHandler>();
        services.AddScoped<GetMemberHandler>();
        services.AddScoped<CreateMemberHandler>();
        services.AddScoped<ImportMembersHandler>();
        services.AddScoped<GrantMemberRoleHandler>();
        services.AddScoped<RevokeMemberRoleHandler>();
        services.AddScoped<ResetMemberPasswordHandler>();
        services.AddScoped<RenameMemberHandler>();
        services.AddScoped<DisableMemberHandler>();
        services.AddScoped<EnableMemberHandler>();
        services.AddScoped<EndMemberSessionsHandler>();
        services.AddScoped<DeleteMemberHandler>();
        services.AddScoped<TransferMemberHandler>();
        services.AddScoped<GetOrganizationSettingsHandler>();
        services.AddScoped<UpdateOrganizationSettingsHandler>();
        services.AddScoped<ListOrganizationChangesHandler>();
        services.AddScoped<ListAccountOrganizationsHandler>();
        services.AddScoped<SwitchActiveOrganizationHandler>();

        return services;
    }

    private static void ConfigureServiceCalls(GrpcServiceOptions options)
    {
        options.MaxReceiveMessageSize = MaximumServiceCallMessageBytes;
        options.CompressionProviders = [new BoundedGzipCompressionProvider(MaximumServiceCallMessageBytes)];
        options.IgnoreUnknownServices = true;
    }

    private static void ConfigureIdentity(IdentityOptions options)
    {
        options.User.AllowedUserNameCharacters = string.Empty;
        options.SignIn.RequireConfirmedAccount = true;
    }

    private static Uri ReadIssuer(IConfiguration configuration) =>
        Uri.TryCreate(configuration[IssuerKey], UriKind.Absolute, out var issuer)
            ? issuer
            : throw new InvalidOperationException($"'{IssuerKey}' must be an absolute URI.");

    private static void AddCredentials(OpenIddictServerBuilder server, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Signing and encryption certificates outside Development are not implemented");
        }

        server.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();
    }

    private static void AddStateTokenCredentials(OpenIddictClientBuilder client, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Certificates that protect external sign-in state outside Development are not implemented.");
        }

        client.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();
    }

    private static string ExternalSignInCallbackPath(IExternalSignInProvider provider) =>
        $"api/v1/account/external-sign-ins/{provider.Name}/callback";

    private static void ConfigureAccessTokenValidation(OpenIddictValidationBuilder validation)
    {
        validation.UseLocalServer();
        validation.AddAudiences(OpenIdNames.IdentityAudience);
        validation.EnableAuthorizationEntryValidation();
        validation.AddEventHandler(ResolveSessionFromAccessToken.Descriptor);

        validation.UseAspNetCore()
            .DisableAccessTokenExtractionFromBodyForm()
            .DisableAccessTokenExtractionFromQueryString();
    }

    private static void ExcludePwnedPasswordsFromTraces(HttpClientTraceInstrumentationOptions options)
    {
        var otherFilter = options.FilterHttpRequestMessage;

        options.FilterHttpRequestMessage = request =>
            request.RequestUri?.Host != PwnedPasswordsClient.Host && (otherFilter?.Invoke(request) ?? true);
    }

    private static void ConfigureIdentityCookie(CookieAuthenticationOptions options)
    {
        options.Cookie.Name = IdentityCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = IdentityCookieLifetime;
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = IdentityCookieValidator.ValidateAsync;
    }

    private static void ConfigurePendingExternalSignInCookie(CookieAuthenticationOptions options)
    {
        options.Cookie.Name = PendingExternalSignInCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = PendingExternalSignInLifetime;
        options.SlidingExpiration = false;
    }

    private static void ConfigureCorrelationCookie(OpenIddictClientAspNetCoreOptions options)
    {
        options.CookieBuilder.Name = CorrelationCookieName;
        options.CookieBuilder.SameSite = SameSiteMode.Lax;
    }
}

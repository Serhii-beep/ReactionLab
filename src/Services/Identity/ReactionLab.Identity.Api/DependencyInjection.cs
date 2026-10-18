using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Instrumentation.Http;
using ReactionLab.Identity.Api.Features.Accounts.RegisterAccount;
using ReactionLab.Identity.Api.Features.Accounts.SignIn;
using ReactionLab.Identity.Api.Features.Accounts.SignOut;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api;

internal static class DependencyInjection
{
    private const string IdentityMeterName = "Microsoft.AspNetCore.Identity";
    private const string IdentityCookieName = "__Host-identity";

    private static readonly TimeSpan IdentityCookieLifetime = TimeSpan.FromMinutes(30);

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddErrorContract();
        services.AddOpenApi();

        services.AddRateLimiter(RateLimitPolicies.Configure);

        return services;
    }

    public static IServiceCollection AddAccounts(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddIdentityCore<User>(ConfigureIdentity)
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();

        services.RemoveAll<IPasswordValidator<User>>();
        services.AddScoped<IPasswordValidator<User>, PasswordRulesValidator>();

        services.Configure<PasswordHasherOptions>(options => options.IterationCount = PasswordPolicy.HashIterations);
        services.Replace(ServiceDescriptor.Singleton<IPasswordHasher<User>, NormalizingPasswordHasher>());
        services.AddSingleton<PasswordVerificationGate>();

        services.AddHttpClient<PwnedPasswordsClient>(PwnedPasswordsClient.Configure).RemoveAllLoggers();
        services.Configure<HttpClientTraceInstrumentationOptions>(ExcludePwnedPasswordsFromTraces);

        services.AddAuthentication().AddIdentityCookies();
        services.ConfigureApplicationCookie(ConfigureIdentityCookie);

        services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(IdentityMeterName, SignInMetrics.MeterName));

        services.AddSingleton<SignInMetrics>();
        services.AddScoped<SignInAttempts>();
        services.AddScoped<RegisterAccountHandler>();
        services.AddScoped<SignInHandler>();
        services.AddScoped<SignOutHandler>();

        return services;
    }

    private static void ConfigureIdentity(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = string.Empty;
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
    }
}

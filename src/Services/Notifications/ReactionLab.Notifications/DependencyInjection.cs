using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Client;
using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Identity.Contracts.SecurityLinks.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Sending;
using ReactionLab.Notifications.ServiceCalls;
using ReactionLab.Notifications.Templates;
using ReactionLab.SharedKernel;

namespace ReactionLab.Notifications;

internal static class DependencyInjection
{
    private const string MailConnectionName = "mail";
    private const int MaximumServiceCallMessageBytes = 64 * 1024;

    private static readonly Uri IdentityAddress = new("https://identity");
    private static readonly Uri IdentityConfigurationEndpoint =
        new(IdentityAddress, ".well-known/openid-configuration");

    public static IHostApplicationBuilder AddIdentityCalls(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<ServiceCallOptions>()
            .BindConfiguration(ServiceCallOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.Issuer is { IsAbsoluteUri: true }, "OpenId:Issuer must be an absolute URI.")
            .ValidateOnStart();

        builder.Services.AddOpenIddict()
            .AddClient(client =>
            {
                client.AllowClientCredentialsFlow()
                    .DisableTokenStorage();

                client.UseSystemNetHttp()
                    .Configure(options => options.HttpResiliencePipeline = null);
            });

        builder.Services.AddOptions<OpenIddictClientOptions>()
            .Configure<IOptions<ServiceCallOptions>>((options, serviceCalls) =>
                options.Registrations.Add(new OpenIddictClientRegistration
                {
                    Issuer = serviceCalls.Value.Issuer,
                    ConfigurationEndpoint = IdentityConfigurationEndpoint,
                    ClientId = serviceCalls.Value.ClientId,
                    ClientSecret = serviceCalls.Value.ClientSecret
                }));

        builder.Services.AddGrpcClient<Contacts.ContactsClient>(options => options.Address = IdentityAddress)
            .ConfigureChannel(channel => channel.MaxReceiveMessageSize = MaximumServiceCallMessageBytes);

        builder.Services.AddGrpcClient<SecurityLinks.SecurityLinksClient>(options => options.Address = IdentityAddress)
            .ConfigureChannel(channel => channel.MaxReceiveMessageSize = MaximumServiceCallMessageBytes);

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ServiceTokenSource>();
        builder.Services.AddScoped<IdentityCalls>();
        builder.Services.AddScoped<ContactDirectory>();
        builder.Services.AddScoped<SecurityLinkIssuer>();
        builder.Services.AddScoped<PasswordChangeDirectory>();

        return builder;
    }

    public static IHostApplicationBuilder AddMailSending(this IHostApplicationBuilder builder)
    {
        MailAddresses.RequireIdnaSupport();
        IanaTimeZones.RequireSupport();

        var developing = builder.Environment.IsDevelopment();

        builder.Services.AddOptions<MailOptions>()
            .BindConfiguration(MailOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => options.SiteUri is { IsAbsoluteUri: true } siteUri
                    && (developing || siteUri.Scheme == Uri.UriSchemeHttps),
                "Mail:SiteUri must be an absolute URI, and https outside Development.")
            .Validate(
                options => Ascii.IsValid(options.FromAddress)
                    && MailAddresses.TryCreateMailbox(options.FromName, options.FromAddress, out _),
                "Mail:FromAddress must be a plain ASCII address.")
            .ValidateOnStart();

        builder.Services.AddMailRendering();
        builder.Services.AddSingleton<IMailTemplate, RepeatedRegistrationMail>();
        builder.Services.AddSingleton<IMailTemplate, EmailConfirmationMail>();
        builder.Services.AddSingleton<IMailTemplate, PasswordResetMail>();
        builder.Services.AddSingleton<IMailTemplate, PasswordChangedMail>();
        builder.Services.AddSingleton<MailTemplates>();
        builder.Services.AddSingleton<MailMetrics>();
        builder.Services.AddSingleton(services => SmtpEndpoint.Parse(
            services.GetRequiredService<IConfiguration>().GetConnectionString(MailConnectionName)
                ?? throw new InvalidOperationException($"Connection string '{MailConnectionName}' is not configured."),
            services.GetRequiredService<IOptions<MailOptions>>().Value));
        builder.Services.AddScoped<MailJobProcessor>();
        builder.Services.AddHostedService<MailJobWorker>();

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(MailTelemetry.SourceName))
            .WithMetrics(metrics => metrics.AddMeter(MailTelemetry.SourceName));

        return builder;
    }
}

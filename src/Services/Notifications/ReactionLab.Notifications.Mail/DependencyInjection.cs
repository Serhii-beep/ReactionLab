using Microsoft.Extensions.DependencyInjection;

namespace ReactionLab.Notifications.Mail;

public static class DependencyInjection
{
    public static IServiceCollection AddMailRendering(this IServiceCollection services)
    {
        services.AddSingleton<ITranslationCatalog, TranslationCatalog>();
        services.AddSingleton<MailRenderer>();

        return services;
    }
}

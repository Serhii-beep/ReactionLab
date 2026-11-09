namespace ReactionLab.Notifications.Mail;

public interface ITranslationCatalog
{
    IReadOnlyCollection<string> Locales { get; }

    string ResolveLocale(string? requestedLocale);

    string Translate(string locale, string key, IReadOnlyDictionary<string, object?>? arguments = null);
}

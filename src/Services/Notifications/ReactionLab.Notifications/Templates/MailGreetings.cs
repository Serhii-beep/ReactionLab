using ReactionLab.Notifications.Mail;

namespace ReactionLab.Notifications.Templates;

internal static class MailGreetings
{
    public static MailText For(ITranslationCatalog catalog, MailRecipient recipient) =>
        recipient.DisplayName is { } displayName
            ? catalog.Phrase(recipient.Locale, "common.greeting", new MailArgument("name", displayName))
            : catalog.Phrase(recipient.Locale, "common.greetingWithoutName");
}

namespace ReactionLab.Notifications.Mail;

public static class TranslationCatalogExtensions
{
    private const char FirstPlaceholder = (char)0xE000;

    public static MailText Phrase(
        this ITranslationCatalog catalog,
        string locale,
        string key,
        params MailArgument[] arguments)
    {
        var placeholders = new Dictionary<string, object?>(arguments.Length, StringComparer.Ordinal);

        for (var index = 0; index < arguments.Length; index++)
        {
            placeholders.Add(arguments[index].Name, ((char)(FirstPlaceholder + index)).ToString());
        }

        var formatted = catalog.Translate(locale, key, placeholders);
        var runs = new List<MailTextRun>();
        var used = new bool[arguments.Length];
        var literalStart = 0;

        for (var position = 0; position < formatted.Length; position++)
        {
            var index = formatted[position] - FirstPlaceholder;

            if (index < 0 || index >= arguments.Length)
            {
                continue;
            }

            if (position > literalStart)
            {
                runs.Add(new MailTextRun(formatted[literalStart..position], IsEmphasized: false));
            }

            runs.Add(new MailTextRun(arguments[index].Value, arguments[index].IsEmphasized));
            used[index] = true;
            literalStart = position + 1;
        }

        if (literalStart < formatted.Length)
        {
            runs.Add(new MailTextRun(formatted[literalStart..], IsEmphasized: false));
        }

        var unused = Array.IndexOf(used, false);

        return unused < 0
            ? new MailText(runs)
            : throw new InvalidOperationException(
                $"The {locale} mail translation '{key}' leaves out the argument '{arguments[unused].Name}'.");
    }
}

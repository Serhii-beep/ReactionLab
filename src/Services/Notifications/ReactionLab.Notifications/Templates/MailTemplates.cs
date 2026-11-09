using System.Collections.Frozen;

namespace ReactionLab.Notifications.Templates;

internal sealed class MailTemplates(IEnumerable<IMailTemplate> templates)
{
    private readonly FrozenDictionary<string, IMailTemplate> _templatesByMailType =
        templates.ToFrozenDictionary(template => template.MailType, StringComparer.Ordinal);

    public IEnumerable<IMailTemplate> All => _templatesByMailType.Values;

    public IMailTemplate Find(string mailType) =>
        _templatesByMailType.TryGetValue(mailType, out var template)
            ? template
            : throw new KeyNotFoundException($"No template writes '{mailType}' mail.");
}

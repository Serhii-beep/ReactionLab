using Jeffijoe.MessageFormat.Formatting;

namespace ReactionLab.Notifications.Mail;

internal sealed class KeyedBlockReader : BaseFormatter
{
    public IEnumerable<KeyedBlock> Read(FormatterRequest request) => ParseArguments(request).KeyedBlocks;
}

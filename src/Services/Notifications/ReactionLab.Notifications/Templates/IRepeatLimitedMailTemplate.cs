namespace ReactionLab.Notifications.Templates;

internal interface IRepeatLimitedMailTemplate
{
    TimeSpan? RepeatIntervalFor(MailDetail detail);
}

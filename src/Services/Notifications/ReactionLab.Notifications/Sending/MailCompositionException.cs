namespace ReactionLab.Notifications.Sending;

internal sealed class MailCompositionException(Exception innerException)
    : Exception("The mail couldn't be composed from its template and translations.", innerException);

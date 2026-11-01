using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace ReactionLab.Messaging.Kafka;

public sealed class KafkaProducerHealth(ILogger<KafkaProducerHealth> logger)
{
    private static readonly ErrorCode[] FatalErrorsARestartKeeps =
    [
        ErrorCode.ClusterAuthorizationFailed,
        ErrorCode.TransactionalIdAuthorizationFailed,
        ErrorCode.Local_UnsupportedFeature
    ];

    private Error? _fatalError;

    public Error? FatalError => Volatile.Read(ref _fatalError);

    public bool RestartClearsFatalError =>
        FatalError is { } fatalError && !FatalErrorsARestartKeeps.Contains(fatalError.Code);

    public void RecordError(Error error)
    {
        if (error.IsFatal)
        {
            Interlocked.CompareExchange(ref _fatalError, error, null);

            return;
        }

        logger.LogWarning("The Kafka producer reported {ErrorCode}: {Reason}", error.Code, error.Reason);
    }
}

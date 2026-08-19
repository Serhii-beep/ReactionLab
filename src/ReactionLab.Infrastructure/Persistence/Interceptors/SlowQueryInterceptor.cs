using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ReactionLab.Infrastructure.Persistence.Interceptors;

internal sealed class SlowQueryInterceptor(
    ILogger<SlowQueryInterceptor> logger,
    TimeSpan threshold) : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Report(command, eventData);

        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Report(command, eventData);

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Report(command, eventData);

        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Report(command, eventData);

        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Report(command, eventData);

        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        Report(command, eventData);

        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void Report(DbCommand command, CommandExecutedEventData eventData)
    {
        if (eventData.Duration < threshold)
        {
            return;
        }

        logger.LogWarning(
            "Query took {ElapsedMilliseconds} ms against a {BudgetMilliseconds} ms budget: {CommandText}",
            eventData.Duration.TotalMilliseconds,
            threshold.TotalMilliseconds,
            command.CommandText);
    }
}

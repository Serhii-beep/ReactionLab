using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ReactionLab.Messaging.Kafka;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Messaging;

public static class DependencyInjection
{
    private const int ProducerLingerMilliseconds = 5;
    private const int ProducerMessageTimeoutMilliseconds = 10_000;

    public static IServiceCollection AddOutbox(this IServiceCollection services, Action<PublishedEventTypes> publish)
    {
        var eventTypes = new PublishedEventTypes();
        publish(eventTypes);

        services.AddSingleton(eventTypes);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<DomainEventOutboxInterceptor>();
        services.AddSingleton<OutboxWriter>();

        return services;
    }

    public static IHostApplicationBuilder AddOutboxRelay<TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        string cloudEventSource)
        where TContext : DbContext
    {
        builder.AddKafkaProducer<string, byte[]?>(
            connectionName,
            settings =>
            {
                settings.DisableHealthChecks = true;
                settings.Config.AllowAutoCreateTopics = false;
                settings.Config.Acks = Acks.All;
                settings.Config.EnableIdempotence = true;
                settings.Config.CompressionType = CompressionType.Gzip;
                settings.Config.LingerMs = ProducerLingerMilliseconds;
                settings.Config.MessageTimeoutMs = ProducerMessageTimeoutMilliseconds;
            },
            (provider, producerBuilder) => producerBuilder.SetErrorHandler(
                (_, error) => provider.GetRequiredService<KafkaProducerHealth>().RecordError(error)));

        builder.Services.AddSingleton<KafkaProducerHealth>();
        builder.Services.Configure<OutboxRelayOptions>(options => options.CloudEventSource = cloudEventSource);
        builder.Services.AddSingleton<OutboxMetrics>();
        builder.Services.AddHostedService<OutboxRelay<TContext>>();
        builder.Services.AddHostedService<StateTopicHeartbeat>();
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(MessagingTelemetry.SourceName))
            .WithMetrics(metrics => metrics.AddMeter(MessagingTelemetry.SourceName));

        return builder;
    }
}

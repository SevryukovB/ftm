using Confluent.Kafka;
using FieldTaskManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldTaskManager.Infrastructure.Messaging;

public sealed class OutboxPublisherHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    IOptions<OutboxPublisherOptions> publisherOptions,
    ILogger<OutboxPublisherHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, publisherOptions.Value.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publishing loop failed.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var batchSize = Math.Clamp(publisherOptions.Value.BatchSize, 1, 500);
        var messages = await context.OutboxMessages
            .Where(m => m.PublishedAt == null && (m.NextAttemptAt == null || m.NextAttemptAt <= now))
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (messages.Count == 0)
        {
            return;
        }

        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            AllowAutoCreateTopics = true
        }).Build();

        foreach (var message in messages)
        {
            try
            {
                await producer.ProduceAsync(
                    kafkaOptions.Value.TaskEventsTopic,
                    new Message<string, string>
                    {
                        Key = message.AggregateId.ToString(),
                        Value = message.Payload
                    },
                    ct);

                message.PublishedAt = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                message.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, message.RetryCount)));
                logger.LogWarning(ex, "Failed to publish outbox message {MessageId}.", message.Id);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}

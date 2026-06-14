using System.Text.Json;
using Confluent.Kafka;
using FieldTaskManager.EarningsService.Entities;
using FieldTaskManager.EarningsService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FieldTaskManager.EarningsService.Messaging;

public sealed class TaskEventsConsumerHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<TaskEventsConsumerHostedService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            try
            {
                consumer.Subscribe(options.Value.TaskEventsTopic);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);
                    await ProcessAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Earnings task event consumer failed. Reconnecting...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally
            {
                consumer.Close();
            }
        }
    }

    private async Task ProcessAsync(string message, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<TaskEventEnvelope>(message, JsonOptions);
        if (envelope is null || envelope.Type != "TaskVerified")
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EarningsDbContext>();

        if (await context.ProcessedEvents.AnyAsync(e => e.EventId == envelope.EventId, ct))
        {
            return;
        }

        var payload = envelope.Payload;
        if (!payload.TryGetProperty("assigneeId", out var assigneeValue) || assigneeValue.ValueKind == JsonValueKind.Null)
        {
            context.ProcessedEvents.Add(new ProcessedEvent { EventId = envelope.EventId, Type = envelope.Type });
            await context.SaveChangesAsync(ct);
            return;
        }

        var amountMinor = payload.TryGetProperty("rewardAmountMinor", out var amountValue)
            ? amountValue.GetInt64()
            : 0;
        var currency = payload.TryGetProperty("rewardCurrency", out var currencyValue)
            ? NormalizeCurrency(currencyValue.GetString())
            : "UAH";

        if (amountMinor <= 0)
        {
            context.ProcessedEvents.Add(new ProcessedEvent { EventId = envelope.EventId, Type = envelope.Type });
            await context.SaveChangesAsync(ct);
            return;
        }

        var userId = assigneeValue.GetGuid();
        var organizationId = payload.GetProperty("organizationId").GetGuid();
        var taskId = payload.GetProperty("taskId").GetGuid();
        var taskTitle = payload.GetProperty("title").GetString() ?? "Task";
        var occurredAt = envelope.OccurredAt;

        context.Transactions.Add(new EarningTransaction
        {
            SourceEventId = envelope.EventId,
            TaskId = taskId,
            TaskTitle = taskTitle,
            UserId = userId,
            OrganizationId = organizationId,
            AmountMinor = amountMinor,
            Currency = currency,
            Type = "TaskReward",
            Status = "Confirmed",
            Description = "Task verified reward",
            OccurredAt = occurredAt
        });

        var balance = await GetOrCreateBalanceAsync(context, userId, organizationId, currency, ct);
        balance.AvailableAmountMinor += amountMinor;
        balance.UpdatedAt = DateTime.UtcNow;

        context.ProcessedEvents.Add(new ProcessedEvent { EventId = envelope.EventId, Type = envelope.Type });
        await context.SaveChangesAsync(ct);
    }

    private static async Task<EarningBalance> GetOrCreateBalanceAsync(
        EarningsDbContext context,
        Guid userId,
        Guid organizationId,
        string currency,
        CancellationToken ct)
    {
        var balance = await context.Balances.FindAsync([userId, organizationId, currency], ct);
        if (balance is not null)
        {
            return balance;
        }

        balance = new EarningBalance
        {
            UserId = userId,
            OrganizationId = organizationId,
            Currency = currency
        };
        context.Balances.Add(balance);
        return balance;
    }

    private static string NormalizeCurrency(string? currency) =>
        currency?.Trim().ToUpperInvariant() is "USD" ? "USD" : "UAH";
}

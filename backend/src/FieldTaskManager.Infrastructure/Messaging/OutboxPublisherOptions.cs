namespace FieldTaskManager.Infrastructure.Messaging;

public sealed class OutboxPublisherOptions
{
    public int BatchSize { get; set; } = 50;
    public int PollIntervalSeconds { get; set; } = 5;
}

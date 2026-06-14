namespace FieldTaskManager.NotificationService.Messaging;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string TaskEventsTopic { get; set; } = "task-events";
    public string ConsumerGroup { get; set; } = "notification-service";
}

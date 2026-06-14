using Microsoft.Extensions.Options;

namespace FieldTaskManager.SchedulerService.Jobs;

public sealed class TaskDeadlineJob(
    HttpClient httpClient,
    IOptions<TaskApiOptions> options,
    ILogger<TaskDeadlineJob> logger)
{
    public async Task MarkNotCompletedAsync(Guid taskId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(options.Value.BaseUrl.TrimEnd('/') + "/"), $"api/internal/tasks/{taskId}/mark-not-completed"));
        request.Headers.Add("X-Internal-Api-Key", options.Value.InternalApiKey);

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning(
                "Deadline job failed for task {TaskId}. Status: {StatusCode}. Body: {Body}",
                taskId,
                response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }

        logger.LogInformation("Deadline job marked task {TaskId} as not completed.", taskId);
    }
}

using FieldTaskManager.SchedulerService.Jobs;
using FieldTaskManager.SchedulerService.Messaging;
using FieldTaskManager.SchedulerService.Scheduling;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection("Scheduler"));
builder.Services.Configure<TaskApiOptions>(builder.Configuration.GetSection("TaskApi"));

var schedulerOptions = builder.Configuration.GetSection("Scheduler").Get<SchedulerOptions>() ?? new SchedulerOptions();
var mongoClient = new MongoClient(schedulerOptions.MongoConnectionString);

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IConfiguration>().GetSection("Scheduler").Get<SchedulerOptions>() ?? new SchedulerOptions();
    return provider.GetRequiredService<IMongoClient>().GetDatabase(options.MongoDatabase);
});

builder.Services.AddHangfire(configuration => configuration.UseMongoStorage(
    mongoClient,
    schedulerOptions.MongoDatabase,
    new MongoStorageOptions
    {
        MigrationOptions = new MongoMigrationOptions
        {
            MigrationStrategy = new MigrateMongoMigrationStrategy(),
            BackupStrategy = new CollectionMongoBackupStrategy()
        },
        CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.Poll,
        QueuePollInterval = TimeSpan.FromSeconds(5)
    }));

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = "ftm-scheduler";
    options.WorkerCount = Math.Max(1, Environment.ProcessorCount);
});

builder.Services.AddHttpClient<TaskDeadlineJob>();
builder.Services.AddSingleton<ITaskScheduleService, TaskScheduleService>();
builder.Services.AddTransient<TaskReminderJob>();
builder.Services.AddHostedService<TaskEventsConsumerHostedService>();

var app = builder.Build();

app.MapHangfireDashboard("/hangfire");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

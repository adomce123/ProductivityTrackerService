using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService;
using ProductivityTrackerService.Core.Configuration;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Core.Services;
using ProductivityTrackerService.Infrastructure.Data;
using ProductivityTrackerService.Infrastructure.Messaging;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.Configure(app => { app.UseHangfireDashboard(); });
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {
        var configuration = hostBuilderContext.Configuration;

        var connectionString = configuration["ConnectionStrings:ProductivityServiceDb"];

        services.Configure<ConsumerConfiguration>(configuration.GetSection("DayEntriesConsumer"));

        JobStorage.Current = new SqlServerStorage(connectionString);

        services.AddDbContextFactory<ProductivityServiceDbContext>(
            options => options.UseSqlServer(connectionString));

        services.AddScoped<IDayEntriesRepository, DayEntriesRepository>();

        services.AddScoped<IDayEntriesService, DayEntriesService>();

        services.AddScoped<IMessageProcessor, MessageProcessorService>();

        services.AddScoped<IKafkaConsumer, KafkaConsumer>();

        services.AddHangfire(config => config.UseSqlServerStorage(connectionString));

        services.AddHangfireServer();

        RecurringJob.AddOrUpdate<IDayEntriesService>(
        "get_all_day_entries",
        _ => _.GetDayEntriesAsync(),
        Cron.Daily());

        services.AddHostedService<MessageConsumerService>();
    })
    .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext())
    .Build();

await host.RunAsync();

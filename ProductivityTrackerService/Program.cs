using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService.Application.Services;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Infrastructure.Configuration;
using ProductivityTrackerService.Infrastructure.Data;
using ProductivityTrackerService.Infrastructure.Data.Repositories;
using ProductivityTrackerService.Infrastructure.Messaging;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.Configure(app => { });
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {
        var configuration = hostBuilderContext.Configuration;

        var connectionString = configuration["ConnectionStrings:ProductivityServiceDb"];

        services.Configure<KafkaSettings>(configuration.GetSection("Kafka").GetSection("Consumers"));

        services.AddDbContext<ProductivityServiceDbContext>(
            options => options.UseSqlServer(connectionString));

        services.AddScoped<IDayEntriesRepository, DayEntriesRepository>();

        services.AddScoped<IDayEntriesService, DayEntriesService>();

        services.AddSingleton<IMessageProcessorService, MessageProcessorService>();

        services.AddSingleton<IKafkaProducer, FailedEntriesProducerService>();

        services.AddHostedService<DayEntryConsumerService>();
        services.AddHostedService<FailedEntriesConsumerService>();

    })
    .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext())
    .Build();

await host.RunAsync();

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductivityTrackerService;
using ProductivityTrackerService.Application.Services;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Infrastructure.Configuration;
using ProductivityTrackerService.Infrastructure.Data;
using ProductivityTrackerService.Infrastructure.Data.Repositories;
using ProductivityTrackerService.Infrastructure.Messaging;
using ProductivityTrackerService.Infrastructure.Messaging.ProductivityTrackerService.Infrastructure.Messaging;
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

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<KafkaSettings>>();

            return new GenericConsumer(
                "DayEntryConsumer",
                serviceProvider.GetRequiredService<IMessageProcessorService>(),
                serviceProvider.GetRequiredService<ILogger<GenericConsumer>>(),
                options.Value.DayEntryConsumerSettings
            );
        });
        //ervices.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<GenericConsumer>());

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<KafkaSettings>>();

            return new GenericConsumer(
                "FailedDayEntryConsumer",
                serviceProvider.GetRequiredService<IMessageProcessorService>(),
                serviceProvider.GetRequiredService<ILogger<GenericConsumer>>(),
                options.Value.FailedDayEntryConsumerSettings
            );
        });

        services.AddMultipleHostedServices(typeof(GenericConsumer));

        services.PrintServices();

    })
    .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext())
    .Build();

await host.RunAsync();

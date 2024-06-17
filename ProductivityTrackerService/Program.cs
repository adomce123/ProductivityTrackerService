using Confluent.Kafka;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductivityTrackerService.Application.Services;
using ProductivityTrackerService.Core.DTOs;
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

        services.AddSingleton<IQueue<DayEntryDto>, ConcurrentQueueWrapper<DayEntryDto>>();

        services.AddSingleton<DayEntryConsumerService>(provider =>
        {
            var messageProcessor = provider.GetRequiredService<IMessageProcessorService>();
            var logger = provider.GetRequiredService<ILogger<DayEntryConsumerService>>();
            var kafkaSettings = provider.GetRequiredService<IOptions<KafkaSettings>>().Value;
            var dayEntrySettings = kafkaSettings.DayEntryConsumerSettings;
            var consumer = new ConsumerBuilder<int, string>(dayEntrySettings.ConsumerConfig).Build();
            return new DayEntryConsumerService(messageProcessor, logger, dayEntrySettings.Topic, consumer);
        });

        services.AddHostedService<DayEntryConsumerService>(provider =>
            provider.GetRequiredService<DayEntryConsumerService>()
        );

        services.AddSingleton<FailedEntriesConsumerService>(provider =>
        {
            var messageProcessor = provider.GetRequiredService<IMessageProcessorService>();
            var logger = provider.GetRequiredService<ILogger<FailedEntriesConsumerService>>();
            var kafkaSettings = provider.GetRequiredService<IOptions<KafkaSettings>>().Value;
            var failedDayEntrySettings = kafkaSettings.FailedDayEntryConsumerSettings;
            var consumer = new ConsumerBuilder<int, string>(failedDayEntrySettings.ConsumerConfig).Build();
            return new FailedEntriesConsumerService(messageProcessor, logger, failedDayEntrySettings.Topic, consumer);
        });

        services.AddHostedService<FailedEntriesConsumerService>(provider =>
            provider.GetRequiredService<FailedEntriesConsumerService>()
        );
    })
    .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext())
    .Build();

await host.RunAsync();

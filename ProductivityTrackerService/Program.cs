using Confluent.Kafka;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

        services.AddSingleton(provider =>
        {
            var kafkaSettings = provider.GetRequiredService<IOptions<KafkaSettings>>().Value;
            var consumerConfig = kafkaSettings.DayEntryConsumerSettings.ConsumerConfig;
            return new ConsumerBuilder<int, string>(consumerConfig).Build();
        });

        services.AddHostedService(provider =>
        {
            var messageProcessor = provider.GetRequiredService<IMessageProcessorService>();
            var logger = provider.GetRequiredService<ILogger<DayEntryConsumerService>>();
            var kafkaSettings = provider.GetRequiredService<IOptions<KafkaSettings>>();
            var consumer = provider.GetRequiredService<IConsumer<int, string>>();
            return new DayEntryConsumerService(messageProcessor, logger, kafkaSettings, consumer);
        });

        services.AddSingleton(provider =>
        {
            var kafkaSettings = provider.GetRequiredService<IOptions<KafkaSettings>>().Value;
            var consumerConfig = kafkaSettings.FailedDayEntryConsumerSettings.ConsumerConfig;
            return new ConsumerBuilder<int, string>(consumerConfig).Build();
        });

        services.AddHostedService(provider =>
        {
            var messageProcessor = provider.GetRequiredService<IMessageProcessorService>();
            var logger = provider.GetRequiredService<ILogger<FailedEntriesConsumerService>>();
            var kafkaSettings = provider.GetRequiredService<IOptions<KafkaSettings>>();
            var consumer = provider.GetRequiredService<IConsumer<int, string>>();
            return new FailedEntriesConsumerService(messageProcessor, logger, kafkaSettings, consumer);
        });
    })
    .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext())
    .Build();

await host.RunAsync();

using ProductivityTrackerService;
using ProductivityTrackerService.Configuration;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostBuilderContext, services) =>
    {
        var dayEntriesConsumerConfig = hostBuilderContext
            .Configuration.GetSection("DayEntriesConsumer")
            .Get<ConsumerConfiguration>();

        services.AddHostedService(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<DayEntryConsumer>>()
            ?? throw new ArgumentException("Logger not implemented");

            return new DayEntryConsumer(logger, dayEntriesConsumerConfig);
        });
    })
    .Build();

await host.RunAsync();

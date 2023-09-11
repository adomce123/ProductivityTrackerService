using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService;
using ProductivityTrackerService.Configuration;
using ProductivityTrackerService.Data;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostBuilderContext, services) =>
    {
        var dayEntriesConsumerConfig = hostBuilderContext
            .Configuration.GetSection("DayEntriesConsumer")
            .Get<ConsumerConfiguration>();

        var configuration = hostBuilderContext.Configuration;

        services.AddDbContext<ProductivityServiceDbContext>(
            options => options.UseSqlServer(configuration["ConnectionStrings:ProductivityServiceDb"]));

        services.AddHostedService(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<DayEntryConsumer>>()
            ?? throw new ArgumentException("Logger not implemented");

            return new DayEntryConsumer(logger, dayEntriesConsumerConfig);
        });
    })
    .Build();

await host.RunAsync();

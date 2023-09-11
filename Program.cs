using ProductivityTrackerService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<DayEntryConsumer>();
    })
    .Build();

await host.RunAsync();

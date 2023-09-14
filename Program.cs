using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService;
using ProductivityTrackerService.Configuration;
using ProductivityTrackerService.Data;
using ProductivityTrackerService.Repositories;
using ProductivityTrackerService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.Configure(app => { app.UseHangfireDashboard(); });
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {
        var dayEntriesConsumerConfig = hostBuilderContext
            .Configuration.GetSection("DayEntriesConsumer")
            .Get<ConsumerConfiguration>();

        var configuration = hostBuilderContext.Configuration;

        var connectionString = configuration["ConnectionStrings:ProductivityServiceDb"];

        JobStorage.Current = new SqlServerStorage(connectionString);

        services.AddDbContext<ProductivityServiceDbContext>(
            options => options.UseSqlServer(connectionString));

        services.AddScoped<IDayEntriesService, DayEntriesService>();

        services.AddScoped<IDayEntriesRepository, DayEntriesRepository>();

        services.AddHangfire(config => config.UseSqlServerStorage(connectionString));

        //RecurringJob.AddOrUpdate<IDayEntriesService>(
        //"get_all_day_entries",
        //_ => _.GetDayEntriesAsync(),
        //Cron.Minutely());

        services.AddHostedService(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<DayEntryConsumer>>()
                ?? throw new ArgumentException("Logger not implemented");

            using (var scope = serviceProvider.CreateScope())
            {
                var dayEntriesService = scope.ServiceProvider.GetRequiredService<IDayEntriesService>();

                return new DayEntryConsumer(logger, dayEntriesConsumerConfig, dayEntriesService);
            }
        });
    })
    .Build();

await host.RunAsync();

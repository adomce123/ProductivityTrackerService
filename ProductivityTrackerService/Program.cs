using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService;
using ProductivityTrackerService.Configuration;
using ProductivityTrackerService.Data;
using ProductivityTrackerService.Repositories;
using ProductivityTrackerService.Services;
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

        services.AddSingleton<IDayEntriesRepository, DayEntriesRepository>();

        services.AddSingleton<IDayEntriesService, DayEntriesService>();

        services.AddSingleton<IMessageProcessor, MessageProcessor>();

        services.AddSingleton<IKafkaConsumer,  KafkaConsumer>();

        services.AddHangfire(config => config.UseSqlServerStorage(connectionString));

        services.AddHangfireServer();

        //RecurringJob.AddOrUpdate<IDayEntriesService>(
        //"get_all_day_entries",
        //_ => _.GetDayEntriesAsync(),
        //Cron.Daily());

        services.AddHostedService<MessageConsumer>();
    })
    .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext())
    .Build();

await host.RunAsync();

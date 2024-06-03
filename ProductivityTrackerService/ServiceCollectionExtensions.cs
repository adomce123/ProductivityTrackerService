namespace ProductivityTrackerService;
public static class ServiceCollectionExtensions
{
    public static void PrintServices(this IServiceCollection services)
    {
        Console.WriteLine("Registered services:");
        foreach (var service in services)
        {
            Console.WriteLine(service.ServiceType.FullName);
        }
    }

    public static void AddMultipleHostedServices(this IServiceCollection services, params Type[] hostedServiceTypes)
    {
        foreach (var hostedServiceType in hostedServiceTypes)
        {
            services.AddSingleton(typeof(IHostedService), hostedServiceType);
        }
    }
}


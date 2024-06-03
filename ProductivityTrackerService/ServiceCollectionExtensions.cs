namespace ProductivityTrackerService;
public static class ServiceCollectionExtensions
{
    public static void PrintRegisteredServices(this IServiceCollection services)
    {
        Console.WriteLine("Registered services:");
        foreach (var service in services)
        {
            Console.WriteLine(service.ServiceType.FullName);
        }
    }
}


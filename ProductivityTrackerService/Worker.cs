using ProductivityTrackerService.Core.Interfaces;

namespace ProductivityTrackerService
{
    public class Worker(
        IEntryPointService _entryPointService, ILogger<Worker> _logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker service starting at: {time}", DateTimeOffset.Now);
            while (!stoppingToken.IsCancellationRequested)
            {
                await _entryPointService.ExecuteAsync();
            }
            _logger.LogInformation("Worker service stopping at: {time}", DateTimeOffset.Now);
        }
    }
}

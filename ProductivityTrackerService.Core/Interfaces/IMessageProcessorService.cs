using Confluent.Kafka;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Interfaces
{
    public interface IMessageProcessorService
    {
        Task ProcessAsync(ConsumeResult<Null, string> response, CancellationToken ct);
    }
}

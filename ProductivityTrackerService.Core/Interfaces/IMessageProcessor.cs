using Confluent.Kafka;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Interfaces
{
    public interface IMessageProcessor
    {
        Task ProcessAsync(ConsumeResult<Null, string> response);
    }
}

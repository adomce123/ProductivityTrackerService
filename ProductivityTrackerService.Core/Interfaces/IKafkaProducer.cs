using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Interfaces
{
    public interface IKafkaProducer
    {
        Task ProduceAsync(string topic, string message);
    }
}
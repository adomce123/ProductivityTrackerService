using Confluent.Kafka;

namespace ProductivityTrackerService.Core.Configuration
{
    public class ConsumerConfiguration
    {
        public string? Topic { get; set; }
        public ConsumerConfig? ConsumerConfig { get; set; }
    }
}

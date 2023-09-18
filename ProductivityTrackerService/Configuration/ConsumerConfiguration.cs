using Confluent.Kafka;

namespace ProductivityTrackerService.Configuration
{
    public class ConsumerConfiguration
    {
        public string? Topic {  get; set; }
        public ConsumerConfig? ConsumerConfig { get; set; }
    }
}

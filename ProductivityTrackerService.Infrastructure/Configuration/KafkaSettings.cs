using Confluent.Kafka;

namespace ProductivityTrackerService.Infrastructure.Configuration
{
    public class KafkaSettings
    {
        public KafkaConsumerSettings DayEntryConsumerSettings { get; set; } = new();
        public KafkaConsumerSettings FailedDayEntryConsumerSettings { get; set; } = new();

    }

    public class KafkaConsumerSettings
    {
        public string? Topic { get; set; }
        public ConsumerConfig? ConsumerConfig { get; set; }
    }
}

using Confluent.Kafka;

namespace ProductivityTrackerService.Infrastructure.Configuration
{
    public class KafkaSettings
    {
        public KafkaConsumerSettings DayEntryConsumerSettings { get; init; } = new();
        public KafkaConsumerSettings FailedDayEntryConsumerSettings { get; init; } = new();

    }

    public class KafkaConsumerSettings
    {
        public string? Topic { get; init; }
        public ConsumerConfig? ConsumerConfig { get; init; }
    }
}

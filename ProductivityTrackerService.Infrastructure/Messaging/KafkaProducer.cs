using Confluent.Kafka;
using Microsoft.Extensions.Options;
using ProductivityTrackerService.Core.Configuration;
using ProductivityTrackerService.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Infrastructure.Messaging
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaProducer(IOptions<ConsumerConfiguration> options)
        {
            var configuration = options.Value;
            var bootstrapServers = configuration.ConsumerConfig?.BootstrapServers;

            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task ProduceAsync(string topic, string message)
        {
            try
            {
                await _producer.ProduceAsync(topic, new Message<Null, string> { Value = message });
            }
            catch (ProduceException<Null, string> ex)
            {
                Console.WriteLine($"Kafka producer failed: {ex.Error.Reason}");
            }
        }
    }
}

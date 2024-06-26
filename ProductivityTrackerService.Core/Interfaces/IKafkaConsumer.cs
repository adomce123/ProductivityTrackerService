﻿using Confluent.Kafka;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Interfaces
{
    public interface IKafkaConsumer
    {
        Task<ConsumeResult<Null, string>?> ConsumeMessageAsync(CancellationToken ct);
        void StoreMessageOffset(ConsumeResult<Null, string> consumeResult);
    }
}

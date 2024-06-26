﻿using Confluent.Kafka;
using ProductivityTrackerService.Core.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Interfaces
{
    public interface IMessageProcessorService
    {
        Task ProcessAsync(ConsumeResult<int, string> response, CancellationToken ct);
        Task InsertDayEntriesBatchInternalAsync(CancellationToken ct);
        Task HandleNotProcessedMessages(List<DayEntryDto>? batch = null);
    }
}

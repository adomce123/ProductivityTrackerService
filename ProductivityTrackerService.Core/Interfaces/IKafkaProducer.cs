using ProductivityTrackerService.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Interfaces
{
    public interface IKafkaProducer
    {
        Task ProduceAsync(IEnumerable<DayEntryDto> batch);
        void Dispose();
    }
}
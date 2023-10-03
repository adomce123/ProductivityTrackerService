using System.Text.Json.Serialization;

namespace ProductivityTrackerService.Models
{
    public class DayEntryDto
    {
        public int Id { get; set; }
        [JsonRequired]
        public DateTime Date { get; set; }
        public string? WeekDay { get; set; }
        [JsonRequired]
        public TimeSpan WakeUpTime { get; set; }
        [JsonRequired]
        public TimeSpan ScreenTime { get; set; }
        [JsonRequired]
        public TimeSpan ProjectWork { get; set; }
        [JsonRequired]
        public bool WentToGym { get; set; }
        [JsonRequired]
        public int Score { get; set; }
        public string? Description { get; set; }
    }
}

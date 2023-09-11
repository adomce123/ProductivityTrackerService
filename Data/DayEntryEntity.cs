namespace ProductivityTrackerService.Data
{
    public class DayEntryEntity
    {
        public int Id { get; set; }
        public DateTime AddedTimestamp { get; set; }
        public DateTime Date { get; set; }
        public string? WeekDay { get; set; }
        public TimeSpan WakeUpTime { get; set; }
        public TimeSpan ScreenTime { get; set; }
        public TimeSpan ProjectWork { get; set; }
        public bool WentToGym { get; set; }
        public int Score { get; set; }
        public string? Description { get; set; }
    }
}

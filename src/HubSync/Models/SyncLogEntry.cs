using System;

namespace HubSync.Models
{
    public class SyncLogEntry
    {
        public int Id { get; set; }
        public string? Owner { get; set; }
        public string? Repository { get; set; }
        public string? User { get; set; }
        public DateTimeOffset Started { get; set; }
        public DateTimeOffset WaterMark { get; set; }
        public DateTimeOffset? Completed { get; set; }
    }
}

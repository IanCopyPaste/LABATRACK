using System;

namespace LABATRACK.models
{
    public class StatusChange
    {
        public int HistoryId { get; set; }
        public int JobId { get; set; }
        public string FromStatus { get; set; }
        public string ToStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public int ChangedByUserId { get; set; }
        public string ChangedBy { get; set; }
        public string Reason { get; set; }
    }
}

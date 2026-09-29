using System;

namespace LABATRACK.Models
{
    /// <summary>One row of JobStatusHistory: a job moved from one status to another.</summary>
    public class StatusChange
    {
        public int HistoryId { get; set; }
        public int JobId { get; set; }
        public string FromStatus { get; set; }   // null on the first row, when the job was created
        public string ToStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public int ChangedByUserId { get; set; }
        public string ChangedBy { get; set; }    // the username
        public string Reason { get; set; }       // required for a failed inspection and a void
    }
}

using System;

namespace LABATRACK.Models
{
    /// <summary>One email attempt to a customer (NotificationLog): Sent, Failed or Skipped.</summary>
    public class NotificationLogEntry
    {
        public const string DropOffKind = "Drop-off";
        public const string ReadyKind = "Ready";

        public const string Sent = "Sent";
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";

        public int NotificationId { get; set; }
        public int JobId { get; set; }
        public string Kind { get; set; }
        public string Recipient { get; set; }
        public string Status { get; set; }
        public string Detail { get; set; }        // the error for a failed send, or why it was skipped
        public bool IsResend { get; set; }
        public DateTime AttemptedAt { get; set; }
        public int AttemptedByUserId { get; set; }
        public string AttemptedBy { get; set; }   // the username
    }
}

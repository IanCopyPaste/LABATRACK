using System;

namespace LABATRACK.models
{
    public class NotificationLogEntry
    {
        public const string DropOffKind = "Drop-off";
        public const string ReadyKind = "Ready";
        public const string ModifiedKind = "Modified";
        public const string VoidedKind = "Voided";

        public const string Sent = "Sent";
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";

        public int NotificationId { get; set; }
        public int JobId { get; set; }
        public string Kind { get; set; }
        public string Recipient { get; set; }
        public string Status { get; set; }
        public string Detail { get; set; }
        public bool IsResend { get; set; }
        public DateTime AttemptedAt { get; set; }
        public int AttemptedByUserId { get; set; }
        public string AttemptedBy { get; set; }
    }
}

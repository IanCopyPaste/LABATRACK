using System;

namespace LABATRACK.models
{
    public class Refund
    {
        public const string Pending = "Pending";
        public const string Released = "Released";

        public const string AtVoid = "At void";
        public const string WithCode = "Code";
        public const string OwnerOverride = "Owner override";

        public int RefundId { get; set; }
        public int JobId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public byte[] CodeHash { get; set; }
        public byte[] CodeSalt { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public string ReleasedBy { get; set; }
        public string ReleasedHow { get; set; }

        public bool IsPending
        {
            get { return Status == Pending; }
        }
    }
}

using System;

namespace LABATRACK.Models
{
    /// <summary>
    /// What the shop owes back to the customer of a voided job that had paid something (the Refunds
    /// table). It is Pending until it is handed over, and Released once, exactly once. While Pending
    /// it carries the hash of the refund code that was emailed to the customer.
    /// </summary>
    public class Refund
    {
        public const string Pending = "Pending";
        public const string Released = "Released";

        // How it was released (ReleasedHow):
        public const string AtVoid = "At void";                 // the customer was at the counter when the job was voided
        public const string WithCode = "Code";                  // the customer came back and gave the refund code
        public const string OwnerOverride = "Owner override";   // the owner released it without the code, giving a reason

        public int RefundId { get; set; }
        public int JobId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public byte[] CodeHash { get; set; }     // null once released
        public byte[] CodeSalt { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public string ReleasedBy { get; set; }   // the username
        public string ReleasedHow { get; set; }

        public bool IsPending
        {
            get { return Status == Pending; }
        }
    }
}

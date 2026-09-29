using System;

namespace LABATRACK.Models
{
    /// <summary>
    /// One row of the Payments table. Amount is what the shop keeps: positive for money in,
    /// negative for a refund. CashReceived is the cash handed over (cash only), so the change
    /// can be shown again later: change = CashReceived - Amount.
    /// </summary>
    public class Payment
    {
        public const string DropOff = "Drop-off";
        public const string Adjustment = "Adjustment";
        public const string VoidRefund = "Void refund";

        public const string Cash = "Cash";
        public const string Ewallet = "E-wallet";

        public int PaymentId { get; set; }
        public int JobId { get; set; }
        public string Kind { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public decimal? CashReceived { get; set; }
        public DateTime PaidAt { get; set; }
        public int RecordedByUserId { get; set; }
        public string RecordedBy { get; set; }   // the username

        // Filled only by the daily ledger query (PaymentRepository.GetBetween).
        public string ClaimNumber { get; set; }
        public string CustomerName { get; set; }
        public string ServiceName { get; set; }
        public decimal WeightKg { get; set; }
        public string Reason { get; set; }        // the void reason, for a void refund

        public bool IsRefund
        {
            get { return Amount < 0; }
        }
    }
}

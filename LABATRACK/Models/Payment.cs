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
        // What the row was (the Kind column):
        public const string DropOff = "Drop-off";          // paid when the job was created: in full, or a down payment
        public const string BalancePayment = "Balance";    // paid later towards what was still owed
        public const string Adjustment = "Adjustment";     // an owner's correction (a refund of what was overpaid)
        public const string VoidRefund = "Void refund";    // everything paid, given back when the job was voided

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
        public decimal JobTotal { get; set; }     // the job's total, to tell a down payment from a full one
        public string Reason { get; set; }        // the void reason, for a void refund

        public bool IsRefund
        {
            get { return Amount < 0; }
        }

        /// <summary>The change given for a cash payment (cash received - amount), or 0.</summary>
        public decimal Change
        {
            get { return CashReceived.HasValue && Amount > 0 ? CashReceived.Value - Amount : 0m; }
        }
    }
}

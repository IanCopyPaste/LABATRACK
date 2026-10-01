using System;

namespace LABATRACK.models
{
    public class Payment
    {
        public const string DropOff = "Drop-off";
        public const string BalancePayment = "Balance";
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
        public string RecordedBy { get; set; }

        public string ClaimNumber { get; set; }
        public string CustomerName { get; set; }
        public string ServiceName { get; set; }
        public decimal WeightKg { get; set; }
        public decimal JobTotal { get; set; }
        public string Reason { get; set; }

        public bool IsRefund
        {
            get { return Amount < 0; }
        }

        public decimal Change
        {
            get
            {
                if (CashReceived.HasValue && Amount > 0)
                    return CashReceived.Value - Amount;
                else
                    return 0m;
            }
        }
    }
}

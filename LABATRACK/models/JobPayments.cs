namespace LABATRACK.models
{
    public class JobPayments
    {
        public int JobId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal CashPaid { get; set; }
        public decimal EwalletPaid { get; set; }
        public int PaymentCount { get; set; }

        public decimal Balance
        {
            get { return TotalAmount - AmountPaid; }
        }
    }
}

namespace LABATRACK.Models
{
    /// <summary>
    /// What has been paid on one job, net of refunds, from the vw_JobPayments view (the one
    /// place this is worked out). AmountPaid never goes above TotalAmount; the difference is
    /// the balance the customer still owes. Once the job is voided, AmountPaid is 0.
    /// </summary>
    public class JobPayments
    {
        public int JobId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal CashPaid { get; set; }
        public decimal EwalletPaid { get; set; }
        public int PaymentCount { get; set; }

        /// <summary>What is still owed: total - paid.</summary>
        public decimal Balance
        {
            get { return TotalAmount - AmountPaid; }
        }
    }
}

namespace LABATRACK.Models
{
    /// <summary>
    /// What has been paid on one job, net of refunds, from the vw_JobPayments view (the one
    /// place this is worked out). After every save, AmountPaid equals TotalAmount, or 0 once
    /// the job is voided.
    /// </summary>
    public class JobPayments
    {
        public int JobId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal CashPaid { get; set; }
        public decimal EwalletPaid { get; set; }
        public int PaymentCount { get; set; }
    }
}

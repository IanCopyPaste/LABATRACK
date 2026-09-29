namespace LABATRACK.Models
{
    /// <summary>
    /// The money moved in one period (usually one day), counted on the payment date: money in
    /// and money refunded, for cash and for e-wallet. The net figures are what the drawer check
    /// and "today's total" use.
    /// </summary>
    public class DaySales
    {
        public decimal CashIn { get; set; }
        public decimal CashRefunds { get; set; }        // a positive number: how much went back out
        public decimal EwalletIn { get; set; }
        public decimal EwalletRefunds { get; set; }
        public int PaymentCount { get; set; }
        public int RefundCount { get; set; }

        public decimal NetCash
        {
            get { return CashIn - CashRefunds; }
        }

        public decimal NetEwallet
        {
            get { return EwalletIn - EwalletRefunds; }
        }

        public decimal NetTotal
        {
            get { return NetCash + NetEwallet; }
        }
    }
}

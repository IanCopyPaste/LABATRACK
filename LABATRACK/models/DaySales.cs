namespace LABATRACK.models
{
    public class DaySales
    {
        public decimal CashIn { get; set; }
        public decimal CashRefunds { get; set; }
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

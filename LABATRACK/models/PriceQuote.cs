using System.Collections.Generic;

namespace LABATRACK.models
{
    public class PriceLine
    {
        public int AddOnId { get; set; }
        public string Name { get; set; }
        public bool IsProduct { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }

        public string Label
        {
            get
            {
                if (IsProduct)
                    return Name + " × " + Quantity;
                else
                    return Name;
            }
        }
    }

    public class PriceQuote
    {
        public PriceQuote()
        {
            Lines = new List<PriceLine>();
        }

        public decimal WeightKg { get; set; }
        public decimal BillableKg { get; set; }
        public decimal RatePerKg { get; set; }
        public decimal MinimumCharge { get; set; }
        public decimal RoundingIncrementKg { get; set; }
        public decimal ChargeBeforeMinimum { get; set; }
        public decimal LaundryCharge { get; set; }
        public List<PriceLine> Lines { get; private set; }
        public decimal Total { get; set; }

        public bool MinimumApplied
        {
            get { return LaundryCharge > ChargeBeforeMinimum; }
        }
    }
}

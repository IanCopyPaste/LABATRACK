using System;
using System.Collections.Generic;
using System.Linq;

namespace LABATRACK.Services
{
    /// <summary>
    /// Works out what a load costs. This is the only place a price is calculated; the page
    /// scripts only show the same sum while the cashier types, and the server always works it
    /// out again here before anything is saved.
    ///
    ///   billable kg    = weight rounded UP to the next multiple of the rounding step
    ///   laundry charge = billable kg x rate per kg, raised to the minimum charge if lower
    ///   add-on line    = quantity x unit price (a service add-on always has quantity 1)
    ///   total          = laundry charge + every add-on line
    ///
    /// Worked example (rate 40, minimum 120, 1 kg steps): 5.1 kg -> 6 kg -> 6 x 40 = 240,
    /// which is above 120; with Extra rinse (20) and Fabric conditioner x 2 (2 x 15 = 30) the
    /// total is 240 + 20 + 30 = 290. A 2.0 kg load is 2 x 40 = 80, raised to the minimum 120.
    ///
    /// The prices passed in are the ones frozen on the job: today's prices for a new job, and
    /// the job's own copied prices when the owner corrects an old one.
    /// </summary>
    public static class PricingService
    {
        /// <summary>
        /// Weight rounded up to the next multiple of the step: 3.2 kg bills as 4 kg with a 1 kg
        /// step, and as 3.5 kg with a 0.5 kg step. A weight already on a step stays as it is.
        /// decimal keeps these exact (3.0 / 0.5 is exactly 6, never 6.0000001).
        /// </summary>
        public static decimal BillableKg(decimal weightKg, decimal roundingIncrementKg)
        {
            return Math.Ceiling(weightKg / roundingIncrementKg) * roundingIncrementKg;
        }

        /// <summary>Billable kilos times the rate, raised to the minimum charge if it comes out lower.</summary>
        public static decimal LaundryCharge(decimal billableKg, decimal ratePerKg, decimal minimumCharge)
        {
            decimal charge = Money(billableKg * ratePerKg);
            return charge < minimumCharge ? minimumCharge : charge;
        }

        /// <summary>True when a saved job's laundry charge was raised to the minimum (billable kg x rate came out lower).</summary>
        public static bool MinimumApplied(decimal billableKg, decimal ratePerKg, decimal laundryCharge)
        {
            return laundryCharge > Money(billableKg * ratePerKg);
        }

        /// <summary>One add-on line: quantity x unit price.</summary>
        public static decimal LineAmount(int quantity, decimal unitPrice)
        {
            return Money(quantity * unitPrice);
        }

        /// <summary>The full breakdown of a load's price, ready to show and to save.</summary>
        public static PriceQuote Quote(decimal weightKg, decimal ratePerKg, decimal minimumCharge,
                                       decimal roundingIncrementKg, IEnumerable<PriceLine> addOns)
        {
            var q = new PriceQuote
            {
                WeightKg = weightKg,
                RatePerKg = ratePerKg,
                MinimumCharge = minimumCharge,
                RoundingIncrementKg = roundingIncrementKg,
            };
            q.BillableKg = BillableKg(weightKg, roundingIncrementKg);
            q.ChargeBeforeMinimum = Money(q.BillableKg * ratePerKg);
            q.LaundryCharge = LaundryCharge(q.BillableKg, ratePerKg, minimumCharge);

            foreach (PriceLine line in addOns ?? Enumerable.Empty<PriceLine>())
            {
                line.Amount = LineAmount(line.Quantity, line.UnitPrice);
                q.Lines.Add(line);
            }
            q.Total = q.LaundryCharge + q.Lines.Sum(l => l.Amount);
            return q;
        }

        /// <summary>
        /// The weight must be more than 0, at most the owner's maximum per job, and have at most
        /// two decimals (it is stored as DECIMAL(6,2)). Checked here on the server for a new job
        /// and for an owner's correction, whatever the page's script allowed.
        /// </summary>
        public static void CheckWeight(decimal weightKg, decimal maxWeightKg)
        {
            if (weightKg <= 0)
                throw new BusinessRuleException("Enter the weight in kilos, for example 5.1.");
            if (weightKg != Math.Round(weightKg, 2))
                throw new BusinessRuleException("Enter the weight with at most two decimals, for example 5.25.");
            if (weightKg > maxWeightKg)
                throw new BusinessRuleException("One job can be at most " + maxWeightKg.ToString("0.##") + " kg. Split a bigger load into two or more jobs.");
        }

        // Pesos have two decimals. Only a 0.5 kg step with a rate like 42.55 can make a third
        // decimal; it is rounded half away from zero (x.xx5 goes up), as people round money.
        private static decimal Money(decimal amount)
        {
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>One add-on on a quote: name, how many, the unit price used, and the line amount.</summary>
    public class PriceLine
    {
        public int AddOnId { get; set; }
        public string Name { get; set; }
        public bool IsProduct { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }   // filled in by PricingService.Quote

        /// <summary>"Fabric conditioner × 2" for a product, just the name for a service add-on.</summary>
        public string Label
        {
            get { return IsProduct ? Name + " × " + Quantity : Name; }
        }
    }

    /// <summary>A load's price, worked out by PricingService.Quote.</summary>
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
        public decimal ChargeBeforeMinimum { get; set; }   // billable kg x rate
        public decimal LaundryCharge { get; set; }         // after the minimum
        public List<PriceLine> Lines { get; private set; }
        public decimal Total { get; set; }

        /// <summary>True when the laundry charge was raised to the minimum.</summary>
        public bool MinimumApplied
        {
            get { return LaundryCharge > ChargeBeforeMinimum; }
        }
    }
}

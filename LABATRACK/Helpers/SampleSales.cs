using System;
using System.Collections.Generic;
using System.Linq;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// DRAFT ONLY: two and a half years of made-up job orders so the Reports page has something
    /// real-looking to chart. Delete this file when Phase 5 builds ReportService on the database.
    ///
    /// It is generated from a fixed seed, so every visit shows the same numbers. The shape
    /// follows what small Philippine laundry shops report: Friday to Sunday are the busiest
    /// days, drop-offs bunch before work and after it, the rainy months (June to October) and
    /// December run above the rest of the year, and a growing share pays by e-wallet.
    /// Sep 22, 2026 (the draft "today") is not random: it holds the same six payments as the
    /// owner dashboard and the daily sales report, so the pages agree.
    /// </summary>
    public static class SampleSales
    {
        public static readonly DateTime Opened = new DateTime(2024, 3, 4);
        public static DateTime Now { get { return SampleData.Now; } }

        public static readonly string[] Staff = { "jcruz", "arivera", "owner" };
        public static readonly string[] Services = { "Wash-Dry-Fold", "Wash-Dry", "Comforter / Bulky" };
        public static readonly string[] AddOnNames = { "Extra rinse", "Stain treatment", "Fabric conditioner", "Detergent sachet" };

        private static readonly Lazy<Data> Cache = new Lazy<Data>(Generate);

        public static IList<SalesJob> Jobs { get { return Cache.Value.Jobs; } }
        public static IList<SalesCustomer> Customers { get { return Cache.Value.Customers; } }

        private class Data
        {
            public List<SalesJob> Jobs = new List<SalesJob>();
            public List<SalesCustomer> Customers = new List<SalesCustomer>();
        }

        // Relative weight of each drop-off hour, 7 AM to 8 PM: a morning peak before work,
        // a quieter afternoon, and an evening peak after it.
        private static readonly double[] HourWeight = { 4, 9, 11, 10, 7, 5, 5, 5, 6, 8, 11, 10, 6, 3 };
        private static readonly double[] WeekdayFactor = { 1.20, 0.90, 0.85, 0.90, 1.00, 1.15, 1.35 };   // Sun..Sat
        private static readonly double[] MonthFactor = { 0.90, 0.92, 0.95, 0.93, 0.95, 1.08, 1.15, 1.18, 1.12, 1.05, 1.00, 1.20 };

        private static readonly string[] FirstNames =
        {
            "Maria", "Jose", "Ana", "Carlo", "Liza", "Ramon", "Grace", "Paolo", "Nena", "Rosa", "Dennis", "Mark",
            "Joy", "Rico", "Aileen", "Jun", "Cristina", "Edwin", "Marites", "Noel", "Lorna", "Arnel", "Divina", "Rey",
            "Kristine", "Allan", "Rowena", "Jerome", "Maricel", "Ronald", "Sheila", "Benjie", "Teresa", "Vilma", "Oscar", "Janet",
        };
        private static readonly string[] LastNames =
        {
            "Santos", "Reyes", "Cruz", "Bautista", "Garcia", "Mendoza", "Villanueva", "Aquino", "Ramos", "Castillo",
            "Flores", "Gonzales", "Del Rosario", "Navarro", "Pascual", "Tolentino", "Dizon", "Salazar", "Manalo", "Soriano",
        };

        private static readonly string[] VoidReasons =
        {
            "Customer changed mind", "Duplicate order, entered twice", "Wrong customer selected", "Customer took the load to another shop",
        };

        private static Data Generate()
        {
            var d = new Data();
            var rng = new Random(20240304);   // fixed seed: the same "history" on every visit

            for (DateTime day = Opened; day < Now.Date; day = day.AddDays(1))
            {
                if ((day.Month == 12 && day.Day == 25) || (day.Month == 1 && day.Day == 1)) continue;   // closed

                double daysOpen = (day - Opened).TotalDays;
                double growth = 4 + 8 * (1 - Math.Exp(-daysOpen / 300));   // a new shop filling up
                double mean = growth * WeekdayFactor[(int)day.DayOfWeek] * MonthFactor[day.Month - 1];
                int count = Math.Max(0, (int)Math.Round(mean * (0.75 + 0.5 * rng.NextDouble())));

                for (int k = 0; k < count; k++)
                    d.Jobs.Add(RandomJob(d, rng, day, daysOpen));
            }

            AddToday(d);
            d.Jobs.Sort((a, b) => a.DroppedOff.CompareTo(b.DroppedOff));
            MarkReturning(d);
            return d;
        }

        private static SalesJob RandomJob(Data d, Random rng, DateTime day, double daysOpen)
        {
            var job = new SalesJob();

            // Drop-off time. Weekend mornings are busier than weekday ones.
            bool weekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
            double[] w = HourWeight.Select((x, i) => weekend && i >= 1 && i <= 4 ? x * 1.3 : x).ToArray();
            int hour = 7 + Pick(rng, w);
            job.DroppedOff = day.AddHours(hour).AddMinutes(rng.Next(60));

            // Service and weight.
            double r = rng.NextDouble();
            job.Service = r < 0.68 ? Services[0] : r < 0.89 ? Services[1] : Services[2];
            double kg = job.Service == Services[2]
                ? 1.5 + rng.NextDouble() * 2.5
                : Math.Exp(Math.Log(4.5) + 0.45 * Normal(rng));
            job.WeightKg = Math.Round((decimal)Math.Min(14, Math.Max(0.8, kg)), 1);

            // Price, the way PricingService will do it: round up, rate, minimum, then add-ons.
            decimal rate = job.Service == Services[0] ? 40 : job.Service == Services[1] ? 35 : 60;
            job.BillableKg = (int)Math.Ceiling(job.WeightKg);
            decimal laundry = job.BillableKg * rate;
            job.MinimumApplied = laundry < 120;
            job.LaundryCharge = Math.Max(120, laundry);

            if (rng.NextDouble() < 0.18) job.AddOns.Add(new SalesAddOn("Extra rinse", 1, 20));
            if (rng.NextDouble() < 0.08) job.AddOns.Add(new SalesAddOn("Stain treatment", 1, 30));
            r = rng.NextDouble();
            int conditioner = r < 0.55 ? 0 : r < 0.80 ? 1 : r < 0.95 ? 2 : 3;
            if (conditioner > 0) job.AddOns.Add(new SalesAddOn("Fabric conditioner", conditioner, 15));
            r = rng.NextDouble();
            int detergent = r < 0.85 ? 0 : r < 0.97 ? 1 : 2;
            if (detergent > 0) job.AddOns.Add(new SalesAddOn("Detergent sachet", detergent, 12));
            job.Total = job.LaundryCharge + job.AddOns.Sum(a => a.Amount);

            // Payment: e-wallet grows from about a fifth to about two fifths.
            job.Method = rng.NextDouble() < 0.22 + 0.18 * Math.Min(1, daysOpen / 930) ? "E-wallet" : "Cash";

            // Staff: the second staff member was hired six months after opening.
            bool twoStaff = day >= Opened.AddMonths(6);
            r = rng.NextDouble();
            job.Staff = !twoStaff ? (r < 0.85 ? "jcruz" : "owner") : (r < 0.50 ? "jcruz" : r < 0.92 ? "arivera" : "owner");

            job.CustomerId = PickCustomer(d, rng, day, daysOpen).Id;

            // About one job in seventy is voided while still Queued, and refunded the same day.
            if (rng.NextDouble() < 0.014)
            {
                job.Voided = true;
                job.VoidReason = VoidReasons[rng.Next(VoidReasons.Length)];
                return job;
            }

            // Turnaround (first Queued to first Ready): about a day, longer on busy weekends,
            // and now and then a day late.
            double hours = 20 + 5 * Normal(rng) + (weekend ? 3 : 0);
            if (rng.NextDouble() < 0.04) hours += 24;
            hours = Math.Min(70, Math.Max(6, hours));
            DateTime ready = job.DroppedOff.AddHours(hours);
            job.ReadyAt = ready <= Now ? ready : (DateTime?)null;
            return job;
        }

        private static SalesCustomer PickCustomer(Data d, Random rng, DateTime day, double daysOpen)
        {
            // Early on most people are new; after a year about three in four orders are repeat
            // business. Earlier customers come back more often, like real regulars.
            double repeat = Math.Min(0.75, 0.20 + 0.55 * daysOpen / 400);
            if (d.Customers.Count > 0 && rng.NextDouble() < repeat)
            {
                double u = rng.NextDouble();
                return d.Customers[(int)(d.Customers.Count * u * u)];
            }
            var c = new SalesCustomer
            {
                Id = d.Customers.Count + 1,
                Name = FirstNames[rng.Next(FirstNames.Length)] + " " + LastNames[rng.Next(LastNames.Length)],
            };
            d.Customers.Add(c);
            return c;
        }

        /// <summary>The draft "today": the six payments every other page shows, up to noon.</summary>
        private static void AddToday(Data d)
        {
            DateTime t = Now.Date;
            d.Jobs.Add(Fixed(d, "Ana Reyes",        t.AddHours(8).AddMinutes(5),   "Comforter / Bulky", 2.0m, 120, 0,  "Cash",     "jcruz", false));
            d.Jobs.Add(Fixed(d, "Carlo Mendoza",    t.AddHours(8).AddMinutes(31),  "Wash-Dry-Fold",     2.3m, 120, 15, "Cash",     "jcruz", false));
            d.Jobs.Add(Fixed(d, "Liza Peñaflor",    t.AddHours(9).AddMinutes(2),   "Wash-Dry",          4.6m, 175, 20, "E-wallet", "jcruz", false));
            d.Jobs.Add(Fixed(d, "Ramon Bautista",   t.AddHours(10).AddMinutes(15), "Wash-Dry-Fold",     7.0m, 280, 0,  "Cash",     "jcruz", false));
            d.Jobs.Add(Fixed(d, "Grace Villanueva", t.AddHours(10).AddMinutes(48), "Wash-Dry-Fold",     1.5m, 120, 0,  "E-wallet", "jcruz", false));
            d.Jobs.Add(Fixed(d, "Mark Tan",         t.AddHours(11).AddMinutes(5),  "Wash-Dry-Fold",     2.5m, 120, 0,  "Cash",     "jcruz", true));
        }

        private static SalesJob Fixed(Data d, string name, DateTime at, string service, decimal kg, decimal laundry, decimal addOn,
                                      string method, string staff, bool voided)
        {
            var c = d.Customers.FirstOrDefault(x => x.Name == name);
            if (c == null)
            {
                c = new SalesCustomer { Id = d.Customers.Count + 1, Name = name };
                d.Customers.Add(c);
            }
            var job = new SalesJob
            {
                DroppedOff = at, Service = service, WeightKg = kg, BillableKg = (int)Math.Ceiling(kg), LaundryCharge = laundry,
                MinimumApplied = laundry == 120 && Math.Ceiling(kg) * (service == "Wash-Dry" ? 35 : service == "Wash-Dry-Fold" ? 40 : 60) < 120,
                Method = method, Staff = staff, CustomerId = c.Id, Voided = voided,
                VoidReason = voided ? "Customer cancelled, wrong bag" : null,
            };
            if (addOn == 15) job.AddOns.Add(new SalesAddOn("Fabric conditioner", 1, 15));
            if (addOn == 20) job.AddOns.Add(new SalesAddOn("Extra rinse", 1, 20));
            job.Total = job.LaundryCharge + job.AddOns.Sum(a => a.Amount);
            return job;
        }

        /// <summary>A job counts as returning business when the customer had an order before it.</summary>
        private static void MarkReturning(Data d)
        {
            var seen = new HashSet<int>();
            foreach (var job in d.Jobs)
            {
                job.IsReturning = !seen.Add(job.CustomerId);
                var c = d.Customers[job.CustomerId - 1];
                if (!job.IsReturning) c.FirstVisit = job.DroppedOff;
            }
        }

        private static int Pick(Random rng, double[] weights)
        {
            double roll = rng.NextDouble() * weights.Sum();
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0) return i;
            }
            return weights.Length - 1;
        }

        private static double Normal(Random rng)
        {
            // Box-Muller: a bell curve from two even random numbers.
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }
    }

    public class SalesJob
    {
        public DateTime DroppedOff { get; set; }       // also the payment time: every job is paid in full at drop-off
        public DateTime? ReadyAt { get; set; }         // first time it reached Ready for pick-up; null if not yet
        public string Service { get; set; }
        public decimal WeightKg { get; set; }
        public int BillableKg { get; set; }
        public decimal LaundryCharge { get; set; }
        public bool MinimumApplied { get; set; }
        public List<SalesAddOn> AddOns { get; private set; }
        public decimal Total { get; set; }
        public string Method { get; set; }
        public string Staff { get; set; }
        public int CustomerId { get; set; }
        public bool IsReturning { get; set; }
        public bool Voided { get; set; }                // refunded in full the same day, so it nets to zero
        public string VoidReason { get; set; }

        public SalesJob() { AddOns = new List<SalesAddOn>(); }
    }

    public class SalesAddOn
    {
        public string Name { get; private set; }
        public int Quantity { get; private set; }
        public decimal Amount { get; private set; }
        public SalesAddOn(string name, int quantity, decimal unitPrice)
        {
            Name = name; Quantity = quantity; Amount = quantity * unitPrice;
        }
    }

    public class SalesCustomer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime FirstVisit { get; set; }
    }
}

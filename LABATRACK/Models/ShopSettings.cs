namespace LABATRACK.Models
{
    /// <summary>
    /// The Settings table (key/value rows) as one object with typed values. The owner changes
    /// these on the Pricing and Settings pages. The values below are only used if a row is
    /// missing; 02_seed_settings.sql puts the real starting values in the table.
    /// </summary>
    public class ShopSettings
    {
        // The keys of the rows in the Settings table.
        public const string MinimumChargeKey = "MinimumCharge";
        public const string RoundingIncrementKgKey = "RoundingIncrementKg";
        public const string MaxWeightKgKey = "MaxWeightKg";
        public const string UnclaimedThresholdDaysKey = "UnclaimedThresholdDays";
        public const string DefaultTurnaroundHoursKey = "DefaultTurnaroundHours";
        public const string ShopNameKey = "ShopName";
        public const string SlipHeaderKey = "SlipHeader";
        public const string ReworkStagesKey = "ReworkStages";

        public ShopSettings()
        {
            MinimumCharge = 120.00m;
            RoundingIncrementKg = 1m;
            MaxWeightKg = 100m;
            UnclaimedThresholdDays = 7;
            DefaultTurnaroundHours = 24;
            ShopName = "LabaTrack Laundry Shop";
            SlipHeader = "";
            ReworkStages = "WDF";
        }

        /// <summary>A laundry charge below this is raised to it (pesos).</summary>
        public decimal MinimumCharge { get; set; }

        /// <summary>The weight is rounded up to the next multiple of this (1 or 0.5 kg).</summary>
        public decimal RoundingIncrementKg { get; set; }

        /// <summary>The heaviest load one job may be; a bigger load is split into two or more jobs.</summary>
        public decimal MaxWeightKg { get; set; }

        /// <summary>A load Ready for pick-up this many days or more is flagged as unclaimed.</summary>
        public int UnclaimedThresholdDays { get; set; }

        /// <summary>Expected pick-up = drop-off time + this many hours (every load, no rush fee).</summary>
        public int DefaultTurnaroundHours { get; set; }

        public string ShopName { get; set; }

        /// <summary>The thank-you line printed on every job slip.</summary>
        public string SlipHeader { get; set; }

        /// <summary>
        /// The work stages a failed inspection may send a load back to, as letters:
        /// W = Washing, D = Drying, F = Folding (open decision 5; default all three).
        /// </summary>
        public string ReworkStages { get; set; }
    }
}

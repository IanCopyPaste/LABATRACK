namespace LABATRACK.models
{
    public class ShopSettings
    {
        public const string MinimumChargeKey = "MinimumCharge";
        public const string RoundingIncrementKgKey = "RoundingIncrementKg";
        public const string MaxWeightKgKey = "MaxWeightKg";
        public const string MinimumDownPaymentPercentKey = "MinimumDownPaymentPercent";
        public const string UnclaimedThresholdDaysKey = "UnclaimedThresholdDays";
        public const string DefaultTurnaroundHoursKey = "DefaultTurnaroundHours";
        public const string ShopNameKey = "ShopName";
        public const string SlipHeaderKey = "SlipHeader";
        public const string ReworkStagesKey = "ReworkStages";

        public static readonly decimal[] RoundingChoices = { 0.1m, 0.2m, 0.3m, 0.4m, 0.5m, 0.6m, 0.7m, 0.8m, 0.9m, 1m };

        public static bool IsValidRounding(decimal step)
        {
            return System.Array.IndexOf(RoundingChoices, step) >= 0;
        }

        public ShopSettings()
        {
            MinimumCharge = 120.00m;
            RoundingIncrementKg = 1m;
            MaxWeightKg = 100m;
            MinimumDownPaymentPercent = 50;
            UnclaimedThresholdDays = 7;
            DefaultTurnaroundHours = 24;
            ShopName = "LabaTrack Laundry Shop";
            SlipHeader = "";
            ReworkStages = "WDF";
        }

        public decimal MinimumCharge { get; set; }

        public decimal RoundingIncrementKg { get; set; }

        public decimal MaxWeightKg { get; set; }

        public int MinimumDownPaymentPercent { get; set; }

        public int UnclaimedThresholdDays { get; set; }

        public int DefaultTurnaroundHours { get; set; }

        public string ShopName { get; set; }

        public string SlipHeader { get; set; }

        public string ReworkStages { get; set; }
    }
}

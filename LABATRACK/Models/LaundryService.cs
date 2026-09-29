namespace LABATRACK.Models
{
    /// <summary>
    /// One row of the Services table: a main service charged per kilo, with its route (which of
    /// the three work stages it includes). Named LaundryService so it is never confused with the
    /// classes in the Services folder.
    /// </summary>
    public class LaundryService
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public decimal RatePerKg { get; set; }
        public bool IncludesWashing { get; set; }
        public bool IncludesDrying { get; set; }
        public bool IncludesFolding { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Jobs with this service not yet Claimed or Voided (worked out by the query).</summary>
        public int LoadsInShop { get; set; }

        /// <summary>A service with a load still in the shop cannot be changed at all.</summary>
        public bool IsInUse
        {
            get { return LoadsInShop > 0; }
        }

        /// <summary>The work stages as letters, in their fixed order: "WDF", "WD", "W".</summary>
        public string StageCode
        {
            get { return (IncludesWashing ? "W" : "") + (IncludesDrying ? "D" : "") + (IncludesFolding ? "F" : ""); }
        }
    }
}

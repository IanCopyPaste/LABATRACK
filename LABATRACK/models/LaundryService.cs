namespace LABATRACK.models
{
    public class LaundryService
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public decimal RatePerKg { get; set; }
        public bool IncludesWashing { get; set; }
        public bool IncludesDrying { get; set; }
        public bool IncludesFolding { get; set; }
        public bool IsActive { get; set; }

        public int LoadsInShop { get; set; }

        public bool IsInUse
        {
            get { return LoadsInShop > 0; }
        }

        public string StageCode
        {
            get { return (IncludesWashing ? "W" : "") + (IncludesDrying ? "D" : "") + (IncludesFolding ? "F" : ""); }
        }
    }
}

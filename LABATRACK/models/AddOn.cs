namespace LABATRACK.models
{
    public class AddOn
    {
        public const string ServiceKind = "Service";
        public const string ProductKind = "Product";

        public int AddOnId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Kind { get; set; }
        public bool IsActive { get; set; }

        public bool IsProduct
        {
            get { return Kind == ProductKind; }
        }
    }
}

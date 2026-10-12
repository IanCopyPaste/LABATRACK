namespace LABATRACK.models
{
    public class JobAddOn
    {
        public int JobAddOnId { get; set; }
        public int JobId { get; set; }
        public int AddOnId { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtCreation { get; set; }

        public bool IsProduct
        {
            get { return Kind == AddOn.ProductKind; }
        }
    }
}

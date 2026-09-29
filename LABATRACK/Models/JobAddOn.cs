namespace LABATRACK.Models
{
    /// <summary>
    /// One add-on line on a job (JobAddOns), with the price copied in when it was added.
    /// The line bills as Quantity x PriceAtCreation (see PricingService.LineAmount).
    /// </summary>
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

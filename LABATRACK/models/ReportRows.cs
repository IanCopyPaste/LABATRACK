using System;
using System.Collections.Generic;

namespace LABATRACK.models
{
    public class ReportPayment
    {
        public DateTime PaidAt { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string Kind { get; set; }
    }

    public class ReportJob
    {
        public ReportJob()
        {
            AddOns = new List<ReportAddOnLine>();
        }

        public int JobId { get; set; }
        public string ClaimNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal WeightKg { get; set; }
        public decimal RatePerKgAtCreation { get; set; }
        public decimal MinimumChargeAtCreation { get; set; }
        public decimal LaundryCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrentStatus { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CreatedByName { get; set; }
        public DateTime? FirstReadyAt { get; set; }
        public bool IsReturning { get; set; }
        public DateTime? CustomerFirstJobAt { get; set; }
        public string VoidReason { get; set; }
        public decimal AmountRefunded { get; set; }
        public decimal RefundPending { get; set; }
        public List<ReportAddOnLine> AddOns { get; private set; }

        public bool IsVoided
        {
            get { return CurrentStatus == "Voided"; }
        }
    }

    public class ReportAddOnLine
    {
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

    public class YearTotal
    {
        public int Year { get; set; }
        public decimal Sales { get; set; }
        public int Orders { get; set; }
    }
}

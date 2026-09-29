using System;
using System.Collections.Generic;

namespace LABATRACK.Models
{
    // The rows the owner's reports are built from (Services/ReportService.cs, Admin/Reports.aspx).
    // They are query results, not tables. Two date rules decide which one a figure uses:
    // money is counted on the day it was paid (ReportPayment.PaidAt); services, add-ons, load
    // sizes, staff and customers are counted on the day the job was created (ReportJob.CreatedAt).

    /// <summary>One payment or refund row: the sales charts sum these by PaidAt.</summary>
    public class ReportPayment
    {
        public DateTime PaidAt { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string Kind { get; set; }
    }

    /// <summary>One job dropped off in the period, with the facts the reports need about it.</summary>
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
        public decimal BillableKg { get; set; }
        public decimal RatePerKgAtCreation { get; set; }
        public decimal MinimumChargeAtCreation { get; set; }
        public decimal LaundryCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrentStatus { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CreatedByName { get; set; }
        public DateTime? FirstReadyAt { get; set; }       // null until it first reaches Ready for pick-up
        public bool IsReturning { get; set; }             // the customer had an earlier job
        public DateTime? CustomerFirstJobAt { get; set; }
        public string VoidReason { get; set; }
        public decimal AmountRefunded { get; set; }       // what its void gave back (0 if nothing was paid)
        public List<ReportAddOnLine> AddOns { get; private set; }

        public bool IsVoided
        {
            get { return CurrentStatus == "Voided"; }
        }
    }

    /// <summary>One add-on line of a job in the period.</summary>
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

    /// <summary>Sales and job orders for one calendar year.</summary>
    public class YearTotal
    {
        public int Year { get; set; }
        public decimal Sales { get; set; }
        public int Orders { get; set; }
    }
}

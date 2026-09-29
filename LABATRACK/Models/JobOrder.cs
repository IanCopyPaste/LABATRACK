using System;

namespace LABATRACK.Models
{
    /// <summary>
    /// One job order (a load), as the screens show it: the JobOrders row plus the few joined
    /// details every screen needs (read from the vw_JobList view).
    /// </summary>
    public class JobOrder
    {
        // ---- The JobOrders row ----
        public int JobId { get; set; }
        public string ClaimNumber { get; set; }
        public int CustomerId { get; set; }
        public int ServiceId { get; set; }
        public decimal WeightKg { get; set; }
        public decimal BillableKg { get; set; }

        // Copied in when the job was created, so a later price change never alters this job.
        public decimal RatePerKgAtCreation { get; set; }
        public decimal MinimumChargeAtCreation { get; set; }
        public decimal RoundingIncrementKgAtCreation { get; set; }
        public decimal LaundryCharge { get; set; }
        public decimal TotalAmount { get; set; }

        // The route, copied from the service when the job was created.
        public bool IncludesWashing { get; set; }
        public bool IncludesDrying { get; set; }
        public bool IncludesFolding { get; set; }

        public string CurrentStatus { get; set; }
        public string Remarks { get; set; }
        public DateTime ExpectedPickupAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? LastEditedAt { get; set; }

        // ---- Joined in by vw_JobList, for display only ----
        public string CustomerName { get; set; }
        public string ContactNumber { get; set; }
        public string CustomerEmail { get; set; }
        public string ServiceName { get; set; }
        public string CreatedByName { get; set; }
        public string LastEditedByName { get; set; }

        /// <summary>When the job reached its current status, who moved it, from where, and why.</summary>
        public DateTime StatusSince { get; set; }
        public string StatusBy { get; set; }
        public string StatusFrom { get; set; }
        public string StatusReason { get; set; }

        /// <summary>Net of refunds, from vw_JobPayments.</summary>
        public decimal AmountPaid { get; set; }

        /// <summary>How the job was paid at drop-off: Cash or E-wallet (null if nothing was paid then).</summary>
        public string PaymentMethod { get; set; }

        /// <summary>
        /// What the customer still owes: the total minus what has been paid. A voided job owes
        /// nothing (what was paid was refunded).
        /// </summary>
        public decimal Balance
        {
            get { return CurrentStatus == Services.StatusService.Voided ? 0m : Math.Max(0m, TotalAmount - AmountPaid); }
        }

        /// <summary>Paid, Partially paid or Unpaid (Services/PaymentService.StatusOf).</summary>
        public string PaymentStatus
        {
            get { return Services.PaymentService.StatusOf(TotalAmount, AmountPaid); }
        }

        public decimal ServiceAddOnAmount { get; set; }
        public decimal ProductAddOnAmount { get; set; }

        /// <summary>The current add-ons as one line: "Extra rinse, Fabric conditioner × 2" (empty if none).</summary>
        public string AddOnSummary { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace LABATRACK.models
{
    public class JobOrder
    {
        public const string PaidStatus = "Paid";
        public const string PartiallyPaidStatus = "Partially paid";
        public const string UnpaidStatus = "Unpaid";

        public int JobId { get; set; }
        public string ClaimNumber { get; set; }
        public int CustomerId { get; set; }
        public int ServiceId { get; set; }
        public decimal WeightKg { get; set; }
        public decimal BillableKg { get; set; }

        public decimal RatePerKgAtCreation { get; set; }
        public decimal MinimumChargeAtCreation { get; set; }
        public decimal RoundingIncrementKgAtCreation { get; set; }
        public decimal LaundryCharge { get; set; }
        public decimal TotalAmount { get; set; }

        public bool IncludesWashing { get; set; }
        public bool IncludesDrying { get; set; }
        public bool IncludesFolding { get; set; }

        public string CurrentStatus { get; set; }
        public string Remarks { get; set; }
        public DateTime ExpectedPickupAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? LastEditedAt { get; set; }

        public string CustomerName { get; set; }
        public string ContactNumber { get; set; }
        public string CustomerEmail { get; set; }
        public string ServiceName { get; set; }
        public string CreatedByName { get; set; }
        public string LastEditedByName { get; set; }

        public DateTime StatusSince { get; set; }
        public string StatusBy { get; set; }
        public string StatusFrom { get; set; }
        public string StatusReason { get; set; }

        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; }

        public decimal Balance
        {
            get
            {
                if (CurrentStatus == JobStatus.Voided)
                    return 0m;
                else
                    return Math.Max(0m, TotalAmount - AmountPaid);
            }
        }

        public string PaymentStatus
        {
            get
            {
                if (AmountPaid >= TotalAmount) return PaidStatus;
                if (AmountPaid > 0) return PartiallyPaidStatus;
                return UnpaidStatus;
            }
        }

        public string RefundStatus { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime? RefundReleasedAt { get; set; }
        public string RefundReleasedBy { get; set; }
        public string RefundReleasedHow { get; set; }
        public int RefundFailedAttempts { get; set; }

        public bool RefundPending
        {
            get { return RefundStatus == Refund.Pending; }
        }

        public decimal ServiceAddOnAmount { get; set; }
        public decimal ProductAddOnAmount { get; set; }

        public string AddOnSummary { get; set; }

        public bool IsInShop
        {
            get { return CurrentStatus != JobStatus.Claimed && CurrentStatus != JobStatus.Voided; }
        }

        public List<string> WorkStages
        {
            get
            {
                List<string> stages = new List<string>();
                if (IncludesWashing) stages.Add(JobStatus.Washing);
                if (IncludesDrying) stages.Add(JobStatus.Drying);
                if (IncludesFolding) stages.Add(JobStatus.Folding);
                return stages;
            }
        }

        public List<string> Route
        {
            get
            {
                List<string> route = new List<string>();
                route.Add(JobStatus.Queued);
                route.AddRange(WorkStages);
                route.Add(JobStatus.Inspection);
                route.Add(JobStatus.Ready);
                route.Add(JobStatus.Claimed);
                return route;
            }
        }

        public string NextStage
        {
            get
            {
                List<string> route = Route;
                int at = route.IndexOf(CurrentStatus);
                if (at >= 0 && at < route.Count - 1)
                    return route[at + 1];
                return null;
            }
        }

        public static JobOrder FromRow(SqlDataReader r)
        {
            JobOrder job = new JobOrder();
            job.JobId = (int)r["JobId"];
            job.ClaimNumber = (string)r["ClaimNumber"];
            job.CustomerId = (int)r["CustomerId"];
            job.CustomerName = (string)r["CustomerName"];
            job.ContactNumber = (string)r["ContactNumber"];
            job.CustomerEmail = r["CustomerEmail"] as string;
            job.ServiceId = (int)r["ServiceId"];
            job.ServiceName = (string)r["ServiceName"];
            job.WeightKg = (decimal)r["WeightKg"];
            job.BillableKg = (decimal)r["BillableKg"];
            job.RatePerKgAtCreation = (decimal)r["RatePerKgAtCreation"];
            job.MinimumChargeAtCreation = (decimal)r["MinimumChargeAtCreation"];
            job.RoundingIncrementKgAtCreation = (decimal)r["RoundingIncrementKgAtCreation"];
            job.LaundryCharge = (decimal)r["LaundryCharge"];
            job.TotalAmount = (decimal)r["TotalAmount"];
            job.IncludesWashing = (bool)r["IncludesWashing"];
            job.IncludesDrying = (bool)r["IncludesDrying"];
            job.IncludesFolding = (bool)r["IncludesFolding"];
            job.CurrentStatus = (string)r["CurrentStatus"];
            job.Remarks = r["Remarks"] as string;
            job.ExpectedPickupAt = (DateTime)r["ExpectedPickupAt"];
            job.CreatedAt = (DateTime)r["CreatedAt"];
            job.CreatedByUserId = (int)r["CreatedByUserId"];
            job.CreatedByName = (string)r["CreatedByName"];
            job.LastEditedAt = r["LastEditedAt"] as DateTime?;
            job.LastEditedByName = r["LastEditedByName"] as string;
            job.StatusSince = (r["StatusSince"] as DateTime?) ?? job.CreatedAt;
            job.StatusBy = r["StatusBy"] as string;
            job.StatusFrom = r["StatusFrom"] as string;
            job.StatusReason = r["StatusReason"] as string;
            job.AmountPaid = (decimal)r["AmountPaid"];
            job.PaymentMethod = r["PaymentMethod"] as string;
            job.RefundStatus = r["RefundStatus"] as string;
            job.RefundAmount = (r["RefundAmount"] as decimal?) ?? 0m;
            job.RefundReleasedAt = r["RefundReleasedAt"] as DateTime?;
            job.RefundReleasedBy = r["RefundReleasedBy"] as string;
            job.RefundReleasedHow = r["RefundReleasedHow"] as string;
            job.RefundFailedAttempts = (r["RefundFailedAttempts"] as int?) ?? 0;
            job.ServiceAddOnAmount = Convert.ToDecimal(r["ServiceAddOnAmount"]);
            job.ProductAddOnAmount = Convert.ToDecimal(r["ProductAddOnAmount"]);
            job.AddOnSummary = (r["AddOnSummary"] as string) ?? "";
            return job;
        }
    }
}

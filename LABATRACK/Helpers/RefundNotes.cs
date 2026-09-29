using System;
using System.Globalization;
using LABATRACK.Models;
using LABATRACK.Services;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// The one-line notes about a voided job's refund that the two dashboards show under "Needs
    /// attention", worded once so the staff board and the owner's dashboard say the same thing.
    /// </summary>
    public static class RefundNotes
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>A job voided today: when, by whom, why, and how its refund stands.</summary>
        public static string ForVoid(JobOrder job)
        {
            return "Voided " + job.StatusSince.ToString("h:mm tt", Inv) + " by " + job.StatusBy
                 + " · “" + job.StatusReason + "” · " + Stand(job);
        }

        /// <summary>A refund that is still waiting from an earlier day: how much and for how long.</summary>
        public static string Waiting(JobOrder job, DateTime now)
        {
            int days = ReportService.DaysWaiting(job.StatusSince, now);
            return "Voided " + job.StatusSince.ToString("MMM d", Inv) + " · “" + job.StatusReason + "” · "
                 + PaymentService.Peso(job.RefundAmount) + " refund waiting " + days + (days == 1 ? " day" : " days");
        }

        private static string Stand(JobOrder job)
        {
            if (job.RefundPending) return PaymentService.Peso(job.RefundAmount) + " refund waiting for the customer";
            if (job.RefundStatus == Refund.Released) return PaymentService.Peso(job.RefundAmount) + " refunded";
            return "nothing was paid";
        }
    }
}

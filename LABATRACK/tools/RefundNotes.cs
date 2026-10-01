using System;
using System.Globalization;
using LABATRACK.models;

namespace LABATRACK.tools
{
    public static class RefundNotes
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string ForVoid(JobOrder job)
        {
            string stand;
            if (job.RefundPending) stand = Money.Peso(job.RefundAmount) + " refund waiting for the customer";
            else if (job.RefundStatus == Refund.Released) stand = Money.Peso(job.RefundAmount) + " refunded";
            else stand = "nothing was paid";

            return "Voided " + job.StatusSince.ToString("h:mm tt", Inv) + " by " + job.StatusBy
                 + " · “" + job.StatusReason + "” · " + stand;
        }

        public static string Waiting(JobOrder job, DateTime now)
        {
            int days = (now.Date - job.StatusSince.Date).Days;
            return "Voided " + job.StatusSince.ToString("MMM d", Inv) + " · “" + job.StatusReason + "” · "
                 + Money.Peso(job.RefundAmount) + " refund waiting " + days + (days == 1 ? " day" : " days");
        }
    }
}

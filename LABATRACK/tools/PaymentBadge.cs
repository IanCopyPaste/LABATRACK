using System.Web;
using LABATRACK.models;

namespace LABATRACK.tools
{
    public static class PaymentBadge
    {
        public static string Html(JobOrder job)
        {
            if (job.CurrentStatus == JobStatus.Voided)
            {
                if (job.RefundPending) return Pill("pill-warn", "Refund waiting · " + Money.Peso(job.RefundAmount));
                return Pill("st-voided", job.RefundStatus == null ? "Voided" : "Refunded");
            }

            if (job.PaymentStatus == JobOrder.PaidStatus)
                return Pill("pill-ok", "Paid in full");
            if (job.PaymentStatus == JobOrder.PartiallyPaidStatus)
                return Pill("pill-warn", "Partial · " + Money.Peso(job.Balance) + " due");
            return Pill("pill-danger", "Unpaid · " + Money.Peso(job.Balance) + " due");
        }

        private static string Pill(string css, string text)
        {
            return "<span class=\"pill " + css + "\">" + HttpUtility.HtmlEncode(text) + "</span>";
        }
    }
}

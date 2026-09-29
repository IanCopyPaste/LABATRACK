using System.Web;
using LABATRACK.Models;
using LABATRACK.Services;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// The small coloured label that says how a job stands on payment, the same on every list
    /// (the owner's dashboard, History): "Paid in full" in green, "Partial · ₱140.00 due" in
    /// amber, "Unpaid · ₱290.00 due" in red. The rule behind it is PaymentService.StatusOf;
    /// this only draws it.
    /// </summary>
    public static class PaymentBadge
    {
        public static string Html(JobOrder job)
        {
            // A voided job gave back whatever was paid on it.
            if (job.CurrentStatus == StatusService.Voided)
                return Pill("st-voided", job.PaymentMethod == null ? "Voided" : "Refunded");

            switch (job.PaymentStatus)
            {
                case PaymentService.Paid:
                    return Pill("pill-ok", "Paid in full");
                case PaymentService.PartiallyPaid:
                    return Pill("pill-warn", "Partial · " + PaymentService.Peso(job.Balance) + " due");
                default:
                    return Pill("pill-danger", "Unpaid · " + PaymentService.Peso(job.Balance) + " due");
            }
        }

        private static string Pill(string css, string text)
        {
            return "<span class=\"pill " + css + "\">" + HttpUtility.HtmlEncode(text) + "</span>";
        }
    }
}

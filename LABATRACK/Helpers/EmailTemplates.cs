using System.Globalization;
using System.Text;
using System.Web;
using LABATRACK.Models;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// The subjects and bodies of the shop's emails (four to customers: drop-off, order updated,
    /// order cancelled and ready for pick-up; one to the owner: the password reset link), built with
    /// StringBuilder (the project has no .html template files). Every value from the database is HTML-encoded, so a name
    /// typed at the counter can never add markup to an email. EmailService sends them.
    /// </summary>
    public static class EmailTemplates
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ------------------------------------------------------------ Drop-off (to the customer)

        public static string DropOffSubject(ShopSettings shop, JobOrder job)
        {
            return shop.ShopName + ": we have your laundry (claim no. " + job.ClaimNumber + ")";
        }

        public static string DropOffBody(ShopSettings shop, JobOrder job)
        {
            var sb = Open(shop);
            sb.Append("<p>Hi ").Append(Enc(job.CustomerName)).Append(",</p>")
              .Append("<p>Thank you for dropping off your laundry. Please keep your claim number; you will need it when you pick it up.</p>")
              .Append(ClaimBox(job.ClaimNumber))
              .Append("<table style=\"border-collapse:collapse; font-size:14px; margin:8px 0 16px;\">");
            Row(sb, "Service", job.ServiceName + " · " + job.WeightKg.ToString("0.0#", Inv) + " kg");
            if (!string.IsNullOrEmpty(job.AddOnSummary)) Row(sb, "Add-ons", job.AddOnSummary);
            if (job.Balance > 0)
            {
                // A down payment (or nothing yet): say what was paid and what is left before pick-up.
                Row(sb, "Total", "₱" + job.TotalAmount.ToString("N2", Inv));
                Row(sb, "Paid", "₱" + job.AmountPaid.ToString("N2", Inv) + (job.PaymentMethod != null ? " (" + job.PaymentMethod + ")" : ""));
                Row(sb, "Balance due", "₱" + job.Balance.ToString("N2", Inv) + ", to be paid before you take your laundry home");
            }
            else
            {
                Row(sb, "Total", "₱" + job.TotalAmount.ToString("N2", Inv) + " (paid in full, " + job.PaymentMethod + ")");
            }
            Row(sb, "Dropped off", job.CreatedAt.ToString("MMM d, yyyy h:mm tt", Inv));
            Row(sb, "Expected pick-up", job.ExpectedPickupAt.ToString("MMM d, yyyy h:mm tt", Inv));
            sb.Append("</table>")
              .Append("<p>We will email you again when it is ready for pick-up.</p>");
            return Close(sb, shop);
        }

        // ------------------------------------------------------------ Ready for pick-up (to the customer)

        public static string ReadySubject(ShopSettings shop, JobOrder job)
        {
            return shop.ShopName + ": your laundry is ready for pick-up (" + job.ClaimNumber + ")";
        }

        public static string ReadyBody(ShopSettings shop, JobOrder job)
        {
            var sb = Open(shop);
            sb.Append("<p>Hi ").Append(Enc(job.CustomerName)).Append(",</p>")
              .Append("<p>Good news: your laundry has passed our final check and is ready for pick-up.</p>")
              .Append(ClaimBox(job.ClaimNumber))
              .Append("<table style=\"border-collapse:collapse; font-size:14px; margin:8px 0 16px;\">");
            Row(sb, "Service", job.ServiceName + " · " + job.WeightKg.ToString("0.0#", Inv) + " kg");
            Row(sb, "Payment", job.Balance > 0
                ? "₱" + job.Balance.ToString("N2", Inv) + " still to pay, when you pick it up."
                : "Paid in full. Nothing more to pay.");
            sb.Append("</table>")
              .Append("<p>Please bring your claim slip or tell us the claim number above.</p>");
            return Close(sb, shop);
        }

        // ------------------------------------------------------------ Order updated (to the customer)

        public static string ModifiedSubject(ShopSettings shop, JobOrder job)
        {
            return shop.ShopName + ": your order was updated (claim no. " + job.ClaimNumber + ")";
        }

        /// <summary>
        /// Sent after the owner corrects a job: what changed, how the order stands now, and what
        /// the customer should do. <paramref name="job"/> is the job as saved, after the correction.
        /// </summary>
        public static string ModifiedBody(ShopSettings shop, JobOrder job, EditSummary summary)
        {
            var sb = Open(shop);
            sb.Append("<p>Hi ").Append(Enc(job.CustomerName)).Append(",</p>")
              .Append("<p>We made a change to your laundry order. This is what changed:</p>")
              .Append(ClaimBox(job.ClaimNumber))
              .Append("<ul style=\"margin:8px 0 16px; padding-left:20px; font-size:14px;\">");
            foreach (string change in summary.Changes)
                sb.Append("<li>").Append(Enc(change)).Append("</li>");
            sb.Append("</ul>");

            sb.Append("<p style=\"margin-bottom:4px;\"><strong>Your order now</strong></p>")
              .Append("<table style=\"border-collapse:collapse; font-size:14px; margin:0 0 16px;\">");
            Row(sb, "Service", job.ServiceName + " · " + job.WeightKg.ToString("0.0#", Inv) + " kg");
            if (!string.IsNullOrEmpty(job.AddOnSummary)) Row(sb, "Add-ons", job.AddOnSummary);
            Row(sb, "Total", "₱" + job.TotalAmount.ToString("N2", Inv));
            Row(sb, "Paid so far", "₱" + job.AmountPaid.ToString("N2", Inv));
            if (job.Balance > 0)
                Row(sb, "Balance due", "₱" + job.Balance.ToString("N2", Inv) + ", to be paid before you take your laundry home");
            Row(sb, "Expected pick-up", job.ExpectedPickupAt.ToString("MMM d, yyyy h:mm tt", Inv));
            sb.Append("</table>");

            if (summary.RefundAmount > 0)
                sb.Append("<p>The new total is lower than what you paid, so <strong>₱").Append(summary.RefundAmount.ToString("N2", Inv))
                  .Append("</strong> is given back to you by ").Append(summary.RefundMethod == Payment.Cash ? "cash" : "e-wallet").Append(".</p>");

            sb.Append("<p style=\"margin-bottom:4px;\"><strong>What to do</strong></p>")
              .Append("<ul style=\"margin:0 0 16px; padding-left:20px; font-size:14px;\">")
              .Append("<li>Keep your claim number. The slip you were given may show the earlier details, so ask us for an updated slip when you pick up your laundry.</li>");
            if (job.Balance > 0)
                sb.Append("<li>The balance of ₱").Append(job.Balance.ToString("N2", Inv)).Append(" is paid at the counter before we hand your laundry over.</li>");
            sb.Append("<li>If this change is not what you expected, please contact the shop.</li></ul>");
            return Close(sb, shop);
        }

        // ------------------------------------------------------------ Order cancelled (to the customer)

        public static string VoidedSubject(ShopSettings shop, JobOrder job)
        {
            return shop.ShopName + ": your order was cancelled (claim no. " + job.ClaimNumber + ")";
        }

        /// <summary>
        /// Sent when a job is voided: the reason, the refund, and what to do next.
        /// <paramref name="refunds"/> are the job's void-refund payments (empty if nothing was paid).
        /// </summary>
        public static string VoidedBody(ShopSettings shop, JobOrder job, System.Collections.Generic.IList<Payment> refunds)
        {
            var sb = Open(shop);
            sb.Append("<p>Hi ").Append(Enc(job.CustomerName)).Append(",</p>")
              .Append("<p>Your laundry order at our shop was cancelled.</p>")
              .Append(ClaimBox(job.ClaimNumber))
              .Append("<table style=\"border-collapse:collapse; font-size:14px; margin:8px 0 16px;\">");
            Row(sb, "Reason", job.StatusReason ?? "");
            Row(sb, "Cancelled", job.StatusSince.ToString("MMM d, yyyy h:mm tt", Inv));
            Row(sb, "Order", job.ServiceName + " · " + job.WeightKg.ToString("0.0#", Inv) + " kg · ₱" + job.TotalAmount.ToString("N2", Inv));
            if (refunds.Count == 0)
            {
                Row(sb, "Refund", "Nothing was paid on this order, so there is nothing to refund.");
            }
            else
            {
                var parts = new System.Collections.Generic.List<string>();
                foreach (Payment refund in refunds)
                    parts.Add("₱" + (-refund.Amount).ToString("N2", Inv) + (refund.Method == Payment.Cash ? " in cash" : " by e-wallet"));
                Row(sb, "Refund", string.Join(" and ", parts) + ", the same way you paid");
            }
            sb.Append("</table>");

            sb.Append("<p style=\"margin-bottom:4px;\"><strong>What to do next</strong></p>")
              .Append("<ul style=\"margin:0 0 16px; padding-left:20px; font-size:14px;\">")
              .Append("<li>This claim number can no longer be used, so please do not bring the slip to claim laundry for this order.</li>");
            if (refunds.Count > 0)
                sb.Append("<li>Your refund is given back the same way you paid. If you have not received it yet, show this email at the counter.</li>");
            sb.Append("<li>If your laundry is still with us and you have not placed a new order for it, please collect it at the counter.</li>")
              .Append("<li>If you did not ask for this, or have any question, please contact the shop.</li></ul>");
            return Close(sb, shop);
        }

        // ------------------------------------------------------------ Password reset (to the owner)

        public static string OwnerResetSubject(ShopSettings shop)
        {
            return shop.ShopName + ": reset your owner password";
        }

        public static string OwnerResetBody(ShopSettings shop, string link, int minutes)
        {
            var sb = Open(shop);
            sb.Append("<p>Someone asked to reset the password of the LabaTrack owner account. If it was you, open this link to choose a new password:</p>")
              .Append("<p style=\"margin:18px 0;\"><a href=\"").Append(HttpUtility.HtmlAttributeEncode(link))
              .Append("\" style=\"display:inline-block; padding:10px 18px; background:#34426b; color:#ffffff; border-radius:6px; text-decoration:none; font-weight:600;\">Choose a new password</a></p>")
              .Append("<p style=\"font-size:13px; color:#4a5768;\">The link works once, within ").Append(minutes).Append(" minutes. ")
              .Append("If you did not ask for this, ignore this email: your password stays as it is.</p>")
              .Append("<p style=\"font-size:12px; color:#7b8796; word-break:break-all;\">").Append(Enc(link)).Append("</p>");
            return Close(sb, shop);
        }

        // ------------------------------------------------------------ Pieces

        private static StringBuilder Open(ShopSettings shop)
        {
            return new StringBuilder()
                .Append("<div style=\"font-family:Segoe UI, Arial, sans-serif; color:#1e2a3a; max-width:520px; line-height:1.5;\">")
                .Append("<div style=\"font-size:18px; font-weight:700; padding-bottom:10px; border-bottom:2px solid #2f62c9; margin-bottom:16px;\">")
                .Append(Enc(shop.ShopName)).Append("</div>");
        }

        private static string Close(StringBuilder sb, ShopSettings shop)
        {
            return sb.Append("<p style=\"font-size:12px; color:#7b8796; margin-top:24px;\">")
                     .Append("This email was sent by ").Append(Enc(shop.ShopName)).Append(". Please do not reply to it.</p></div>")
                     .ToString();
        }

        private static string ClaimBox(string claimNumber)
        {
            return "<div style=\"display:inline-block; margin:6px 0 12px; padding:10px 16px; border:1.5px solid #1e2a3a; border-radius:6px;\">"
                 + "<div style=\"font-size:11px; letter-spacing:.12em; text-transform:uppercase; color:#4a5768;\">Claim number</div>"
                 + "<div style=\"font-family:Consolas, monospace; font-size:22px; font-weight:700;\">" + Enc(claimNumber) + "</div></div>";
        }

        private static void Row(StringBuilder sb, string label, string value)
        {
            sb.Append("<tr><td style=\"padding:3px 16px 3px 0; color:#7b8796;\">").Append(Enc(label))
              .Append("</td><td style=\"padding:3px 0;\">").Append(Enc(value)).Append("</td></tr>");
        }

        private static string Enc(string s)
        {
            return HttpUtility.HtmlEncode(s ?? "");
        }
    }
}

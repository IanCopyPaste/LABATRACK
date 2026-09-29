using System.Globalization;
using System.Text;
using System.Web;
using LABATRACK.Models;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// The subjects and bodies of the shop's three emails, built with StringBuilder (the project
    /// has no .html template files). Every value from the database is HTML-encoded, so a name
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
            Row(sb, "Total", "₱" + job.TotalAmount.ToString("N2", Inv) + " (paid in full, " + job.PaymentMethod + ")");
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
            Row(sb, "Payment", "Paid in full at drop-off. Nothing more to pay.");
            sb.Append("</table>")
              .Append("<p>Please bring your claim slip or tell us the claim number above.</p>");
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

using System;
using System.Globalization;
using LABATRACK.Security;

namespace LABATRACK.Pages.Shared
{
    // DRAFT: design only. The slip shows the sample job; the payment comes from the
    // query string (?method=Cash&cash=500.00) so the change can be tried from NewJob.
    // Phase 3 loads the job and its payment by JobId instead.
    public partial class JobSlip : BasePage
    {
        protected override string PageStyleSheet
        {
            get { return "JobSlip.css"; }
        }

        protected decimal Total = 305.00m;
        protected string Method { get; private set; }
        protected decimal CashReceived { get; private set; }
        protected decimal Change { get { return CashReceived - Total; } }   // change = cash received - total

        protected void Page_Load(object sender, EventArgs e)
        {
            Method = Request.QueryString["method"] == "E-wallet" ? "E-wallet" : "Cash";

            decimal cash;
            if (!decimal.TryParse(Request.QueryString["cash"], NumberStyles.Number, CultureInfo.InvariantCulture, out cash)
                || cash < Total)
                cash = 500.00m;   // sample value when opened without a valid amount
            CashReceived = cash;
        }

        protected static string Money(decimal amount)
        {
            return amount.ToString("N2");
        }
    }
}

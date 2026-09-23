using System;
using System.Globalization;
using LABATRACK.Security;

namespace LABATRACK.Pages.Shared
{
    // DRAFT: design only. The page shows sample data until Phase 3 is built.
    public partial class NewJob : BasePage
    {
        protected override string PageStyleSheet
        {
            get { return "NewJob.css"; }
        }

        private const decimal SampleTotal = 290.00m;   // DRAFT: Phase 3 gets this from PricingService

        protected override string ActiveNav
        {
            get { return "NewJob"; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            if (rbEwallet.Checked)
            {
                // E-wallet is always the exact total, so there is no cash or change.
                Response.Redirect("JobSlip.aspx?method=E-wallet");
                return;
            }

            // Never trust the change shown by the browser: check the cash again here.
            decimal cashReceived;
            string typed = txtCashReceived.Text.Replace(",", "").Trim();
            if (!decimal.TryParse(typed, NumberStyles.Number, CultureInfo.InvariantCulture, out cashReceived)
                || cashReceived < SampleTotal)
            {
                lblPaymentError.Text = "Cash received must be at least the total of ₱" + SampleTotal.ToString("N2") + ".";
                lblPaymentError.Visible = true;
                return;
            }

            // DRAFT: the slip reads the cash from the query string. Phase 3 saves it in
            // Payments.CashReceived and the slip loads it by JobId instead.
            Response.Redirect("JobSlip.aspx?method=Cash&cash=" + cashReceived.ToString("0.00", CultureInfo.InvariantCulture));
        }
    }
}

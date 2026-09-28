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

        protected override string ActiveNav
        {
            get { return "NewJob"; }
        }

        // DRAFT: the maximum weight per job. Phase 3 reads it from Settings, where the owner sets
        // it on the Pricing page. The page script shows the limit; the check below is the rule.
        protected const int SampleMaxWeightKg = 100;

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            decimal weight;
            if (!decimal.TryParse(txtWeight.Text.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out weight)
                || weight <= 0 || weight > SampleMaxWeightKg)
            {
                lblPaymentError.Text = "Enter a weight above 0 kg and at most " + SampleMaxWeightKg + " kg.";
                lblPaymentError.Visible = true;
                return;
            }

            // DRAFT: the total the page showed. Phase 3 never uses a number from the browser:
            // it works the total out again with PricingService from the service, the weight
            // and the add-ons, and checks the cash against that.
            decimal total;
            if (!decimal.TryParse(hfShownTotal.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out total) || total <= 0)
            {
                lblPaymentError.Text = "Enter the weight so the total can be worked out.";
                lblPaymentError.Visible = true;
                return;
            }

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
                || cashReceived < total)
            {
                lblPaymentError.Text = "Cash received must be at least the total of ₱" + total.ToString("N2") + ".";
                lblPaymentError.Visible = true;
                return;
            }

            // DRAFT: the slip reads the cash from the query string. Phase 3 saves it in
            // Payments.CashReceived and the slip loads it by JobId instead.
            Response.Redirect("JobSlip.aspx?method=Cash&cash=" + cashReceived.ToString("0.00", CultureInfo.InvariantCulture));
        }
    }
}

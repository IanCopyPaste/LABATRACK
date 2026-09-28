using System;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    // DRAFT: design only. ?case=refund previews an edit that lowers the total
    // (customer overpaid); without it the page previews an edit that raises it.
    public partial class EditJob : BasePage
    {
        protected bool IsRefund { get; private set; }

        // DRAFT: the sample job is in Washing. Phase 4 reads the job's current status.
        private const string SampleStage = "Washing";

        // The service can change only while the job is still Queued: a different service can
        // have a different route, and a load already in, say, Folding may not have that stage
        // on the new one. Weight, add-on and customer fixes stay allowed until it is claimed.
        protected bool ServiceChangeAllowed { get { return SampleStage == "Queued"; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            IsRefund = Request.QueryString["case"] == "refund";
            if (!IsPostBack)
                txtWeight.Text = IsRefund ? "2.9" : "5.3";
            ddlService.Enabled = ServiceChangeAllowed;
        }
    }
}

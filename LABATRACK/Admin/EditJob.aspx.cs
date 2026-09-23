using System;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    // DRAFT: design only. ?case=refund previews an edit that lowers the total
    // (customer overpaid); without it the page previews an edit that raises it.
    public partial class EditJob : BasePage
    {
        protected bool IsRefund { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            IsRefund = Request.QueryString["case"] == "refund";
            if (!IsPostBack)
                txtWeight.Text = IsRefund ? "2.9" : "5.3";
        }
    }
}

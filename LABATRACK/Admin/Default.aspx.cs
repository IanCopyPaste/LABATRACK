using System;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    public partial class Default : AdminBasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect("~/Admin/Dashboard.aspx");
        }
    }
}

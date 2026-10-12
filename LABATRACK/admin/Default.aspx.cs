using System;
using LABATRACK.security;

namespace LABATRACK.admin
{
    public partial class Default : AdminBasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect("~/admin/Dashboard.aspx");
        }
    }
}

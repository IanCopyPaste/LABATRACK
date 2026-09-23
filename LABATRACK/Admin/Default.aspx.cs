using System;
using System.Web.UI;

namespace LABATRACK.Admin
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect("~/Admin/Dashboard.aspx");
        }
    }
}

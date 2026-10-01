using System;
using System.Web.UI;

namespace LABATRACK
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string role = Session["Role"] as string;

            if (role == "Owner")
                Response.Redirect("~/admin/Dashboard.aspx");
            else if (role == "Staff")
                Response.Redirect("~/pages/shared/Dashboard.aspx");
            else
                Response.Redirect("~/Login.aspx");
        }
    }
}

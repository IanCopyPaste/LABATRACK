using System;
using System.Web.UI;

namespace LABATRACK
{
    public partial class Logout : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string role = Session["Role"] as string;
            Session.Abandon();

            if (role == "Owner")
                Response.Redirect("~/admin/Login.aspx");
            else
                Response.Redirect("~/Login.aspx");
        }
    }
}

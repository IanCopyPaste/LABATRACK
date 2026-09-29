using System;
using System.Web.UI;
using LABATRACK.Models;
using LABATRACK.Services;

namespace LABATRACK
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // The front door: a signed-in user goes to their own dashboard, anyone else to the
            // staff login, which links to the owner's.
            UserAccount user = AuthService.CurrentUser;
            if (user == null)
                Response.Redirect("~/Login.aspx");
            else if (user.IsOwner)
                Response.Redirect("~/Admin/Dashboard.aspx");
            else
                Response.Redirect("~/Pages/Shared/Dashboard.aspx");
        }
    }
}

using System;
using System.Web.UI;
using LABATRACK.Models;
using LABATRACK.Services;

namespace LABATRACK
{
    public partial class Logout : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Back to the login page the account came from: the owner's for the owner.
            UserAccount user = AuthService.CurrentUser;
            AuthService.SignOut();
            Session.Abandon();
            Response.Redirect(user != null && user.IsOwner ? "~/Admin/Login.aspx" : "~/Login.aspx");
        }
    }
}

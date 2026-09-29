using System;
using System.Web;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK
{
    // Staff sign-in. The owner signs in on Admin/Login.aspx; both use AuthService.
    public partial class Login : BasePage
    {
        protected override bool AllowAnonymous
        {
            get { return true; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Forms authentication knows only this login page, so a visitor stopped at an owner
            // page lands here. Send them on to the owner's login, keeping where they were going.
            string returnUrl = SafeReturnUrl();
            if (returnUrl != null && returnUrl.StartsWith(VirtualPathUtility.ToAbsolute("~/Admin/"), StringComparison.OrdinalIgnoreCase))
                Response.Redirect("~/Admin/Login.aspx?ReturnUrl=" + HttpUtility.UrlEncode(returnUrl), true);

            // Already signed in: go to your own dashboard.
            if (!IsPostBack && CurrentUser != null)
                Response.Redirect("~/Default.aspx", true);

            // Opening the site's plain address (the start page) brings the visitor here with
            // ReturnUrl=/ attached; there is nowhere to return to, so show the clean address.
            if (!IsPostBack && returnUrl == VirtualPathUtility.ToAbsolute("~/"))
                Response.Redirect("~/Login.aspx", true);
        }

        protected void btnSignIn_Click(object sender, EventArgs e)
        {
            // AuthService gives the same answer to every failure: an unknown username, a wrong
            // password, a locked or disabled account, or the owner's account used on this page.
            UserAccount user = AuthService.SignIn(txtUsername.Text, txtPassword.Text, UserAccount.StaffRole);
            if (user == null)
            {
                lblError.Text = AuthService.InvalidSignIn;
                pnlError.Visible = true;
                return;
            }

            Response.Redirect(SafeReturnUrl() ?? "~/Pages/Shared/Dashboard.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}

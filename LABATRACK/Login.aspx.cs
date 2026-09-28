using System;
using LABATRACK.Helpers;
using LABATRACK.Security;

namespace LABATRACK
{
    // Staff sign-in. The owner signs in on Admin/Login.aspx; both will use one AuthService.
    public partial class Login : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnSignIn_Click(object sender, EventArgs e)
        {
            // DRAFT: no password check yet, so the staff version of the app can be clicked
            // through. Phase 2 replaces this with AuthService: the same single message
            // "Invalid username or password" on any failure, a lock after five wrong tries,
            // and a staff account can never sign in here as the owner.
            string username = txtUsername.Text.Trim().ToLowerInvariant();
            SampleData.SignIn(Session, username == "" ? "jcruz" : username, false);
            Response.Redirect("~/Pages/Shared/Dashboard.aspx");
        }
    }
}

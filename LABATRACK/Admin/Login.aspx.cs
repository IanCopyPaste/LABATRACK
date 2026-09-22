using System;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    public partial class Login : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnSignIn_Click(object sender, EventArgs e)
        {
            // DRAFT: no password check yet, so the design flow can be clicked through.
            // Phase 2 replaces this with AuthService, and on failure shows pnlError
            // with the single message "Invalid username or password".
            Response.Redirect("~/Admin/Dashboard.aspx");
        }
    }
}

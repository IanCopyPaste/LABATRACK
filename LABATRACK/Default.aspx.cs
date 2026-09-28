using System;
using System.Web.UI;
using LABATRACK.Helpers;

namespace LABATRACK
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // The front door: a signed-in user goes to their own dashboard, anyone else to the
            // staff login, which links to the owner's. DRAFT: the sign-in is the draft one in
            // SampleData; Phase 2 reads it from Forms authentication and AuthService.
            if (!SampleData.IsSignedIn(Session))
                Response.Redirect("~/Login.aspx");
            else if (SampleData.IsOwner(Session))
                Response.Redirect("~/Admin/Dashboard.aspx");
            else
                Response.Redirect("~/Pages/Shared/Dashboard.aspx");
        }
    }
}

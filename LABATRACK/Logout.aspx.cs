using System;
using System.Web.UI;

namespace LABATRACK
{
    public partial class Logout : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // DRAFT: nothing to clear yet. Phase 2 adds FormsAuthentication.SignOut() here.
            Session.Abandon();
            Response.Redirect("~/Default.aspx");
        }
    }
}

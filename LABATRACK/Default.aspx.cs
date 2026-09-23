using System;
using System.Web.UI;

namespace LABATRACK
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // DRAFT: always goes to the owner login. Phase 2 routes staff to ~/Login.aspx
            // and signed-in users straight to their dashboard.
            Response.Redirect("~/Admin/Login.aspx");
        }
    }
}

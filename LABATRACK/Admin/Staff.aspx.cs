using System;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    // DRAFT: design only. The page shows sample data until its phase is built.
    public partial class Staff : BasePage
    {
        protected override string ActiveNav
        {
            get { return "Staff"; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}

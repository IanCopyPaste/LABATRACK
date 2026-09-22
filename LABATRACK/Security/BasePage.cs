using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using LABATRACK.Helpers;

namespace LABATRACK.Security
{
    /// <summary>
    /// Base class for every page. For now it only applies the shared layout:
    /// the shared styles from SiteStyle go into &lt;head&gt; and the menu goes into the page's litNav Literal.
    /// DRAFT: the sign-in check is added in Phase 2 (and AdminBasePage for Admin/ pages).
    /// </summary>
    public class BasePage : Page
    {
        /// <summary>Key of the menu item to highlight. Pages override this.</summary>
        protected virtual string ActiveNav
        {
            get { return ""; }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Header != null)
                Header.Controls.AddAt(0, new LiteralControl(SiteStyle.StyleTag()));

            // Login and printable pages have no litNav, so they get no menu.
            var nav = FindControl("litNav") as Literal;
            if (nav != null)
                nav.Text = NavBar.Render(ActiveNav);
        }
    }
}

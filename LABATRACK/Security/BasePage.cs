using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Services;

namespace LABATRACK.Security
{
    /// <summary>
    /// Base class for every page. It does three things:
    ///   1. Lets only a signed-in, active account run the page (the login pages opt out with
    ///      AllowAnonymous). Anyone else is sent to the login page and the page never runs.
    ///   2. Knows who is signed in (CurrentUser, IsOwner), so pages can record who did what.
    ///   3. Applies the shared layout: the stylesheets from SiteStyle go into &lt;head&gt; and the
    ///      menu for the user's role goes into the page's litNav Literal.
    /// Pages in Admin/ inherit AdminBasePage instead, which allows the owner only.
    /// </summary>
    public class BasePage : Page
    {
        /// <summary>Key of the menu item to highlight. Pages override this.</summary>
        protected virtual string ActiveNav
        {
            get { return ""; }
        }

        /// <summary>
        /// File name in ~/Assets/css/pages for rules only this page needs (for example "JobDetail.css").
        /// Pages without their own stylesheet leave it empty.
        /// </summary>
        protected virtual string PageStyleSheet
        {
            get { return ""; }
        }

        /// <summary>True only on the two login pages, which must open for someone not signed in.</summary>
        protected virtual bool AllowAnonymous
        {
            get { return false; }
        }

        /// <summary>The signed-in account, or null (only possible on the login pages).</summary>
        protected UserAccount CurrentUser
        {
            get { return AuthService.CurrentUser; }
        }

        protected bool IsOwner
        {
            get { return CurrentUser != null && CurrentUser.IsOwner; }
        }

        protected override void OnInit(EventArgs e)
        {
            CheckAccess();

            // Ties every form post to the account that loaded the page, so a post forged from
            // another site (or another account's page) fails ASP.NET's ViewState check.
            if (CurrentUser != null)
                ViewStateUserKey = CurrentUser.Username;

            base.OnInit(e);
        }

        /// <summary>Who may run this page. AdminBasePage makes it stricter.</summary>
        protected virtual void CheckAccess()
        {
            if (!AllowAnonymous && CurrentUser == null)
                RedirectToLogin("~/Login.aspx");
        }

        /// <summary>
        /// Sends the visitor to a login page and stops this page. ReturnUrl brings them back here
        /// after signing in. A cookie left from an account that has since been disabled is cleared.
        /// </summary>
        protected void RedirectToLogin(string loginPage)
        {
            if (Request.IsAuthenticated && CurrentUser == null)
                AuthService.SignOut();
            Response.Redirect(loginPage + "?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl), true);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Header != null)
                Header.Controls.AddAt(0, new LiteralControl(SiteStyle.StyleTag(PageStyleSheet)));

            // Login and printable pages have no litNav, so they get no menu.
            var nav = FindControl("litNav") as Literal;
            if (nav != null && CurrentUser != null)
                nav.Text = NavBar.Render(ActiveNav, CurrentUser);
        }

        /// <summary>
        /// A local address to go back to after signing in, or null. Only paths inside this site
        /// are accepted, so a link cannot send someone to another website after they sign in.
        /// </summary>
        protected string SafeReturnUrl()
        {
            string url = Request.QueryString["ReturnUrl"];
            if (string.IsNullOrEmpty(url) || !url.StartsWith("/") || url.StartsWith("//") || url.StartsWith("/\\"))
                return null;
            return url;
        }
    }
}

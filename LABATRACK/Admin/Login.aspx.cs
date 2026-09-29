using System;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Admin
{
    // The owner's sign-in, and the owner's "Forgot password?" flow. This is the only page in
    // Admin/ that anyone may open (Web.config allows it), so it inherits BasePage, not AdminBasePage.
    public partial class Login : BasePage
    {
        protected override bool AllowAnonymous
        {
            get { return true; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // One card, four states, picked by the address: sign in, ask for a link (?forgot=1),
            // choose a new password (?token=...), or a link that no longer works.
            string token = Request.QueryString["token"];
            bool forgot = Request.QueryString["forgot"] == "1";
            bool tokenWorks = token != null && AuthService.FindByResetToken(token) != null;

            pnlSignIn.Visible = token == null && !forgot;
            pnlForgot.Visible = token == null && forgot;
            pnlReset.Visible = tokenWorks;
            pnlExpired.Visible = token != null && !tokenWorks;
            pnlResetDone.Visible = !IsPostBack && Request.QueryString["reset"] == "done";

            // Enter presses the button of the state on screen.
            if (pnlForgot.Visible) form1.DefaultButton = btnSendReset.ID;
            if (pnlReset.Visible) form1.DefaultButton = btnSetPassword.ID;

            if (!IsPostBack && pnlSignIn.Visible && CurrentUser != null)
            {
                // The owner is already signed in: go on. A staff account was sent here by an owner
                // page it may not open, so it is told to sign in with the owner account instead.
                if (CurrentUser.IsOwner)
                    Response.Redirect(SafeReturnUrl() ?? "~/Admin/Dashboard.aspx", true);
                litSignedInAs.Text = HttpUtility.HtmlEncode(CurrentUser.Username);
                pnlStaffSignedIn.Visible = true;
            }
        }

        protected void btnSignIn_Click(object sender, EventArgs e)
        {
            // The same message for every failure, including a staff account on this page.
            UserAccount user = AuthService.SignIn(txtUsername.Text, txtPassword.Text, UserAccount.OwnerRole);
            if (user == null)
            {
                lblError.Text = AuthService.InvalidSignIn;
                pnlError.Visible = true;
                return;
            }

            Response.Redirect(SafeReturnUrl() ?? "~/Admin/Dashboard.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // Step 1: only an active Owner account with a recovery email gets a link (AuthService).
        // For a staff account, an unknown username or no email, nothing is sent, and the page says
        // the same thing either way, so it never shows which accounts exist.
        protected void btnSendReset_Click(object sender, EventArgs e)
        {
            if (txtResetUsername.Text.Trim() == "")
            {
                ShowError(lblResetUsernameError, "Enter your username.");
                return;
            }

            string loginUrl = Request.Url.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute("~/Admin/Login.aspx");
            AuthService.RequestOwnerReset(txtResetUsername.Text, loginUrl);
            pnlForgotForm.Visible = false;
            pnlSent.Visible = true;
        }

        // Step 2: the page the emailed link opens. AuthService checks the link again on this click,
        // saves the new password with a new salt, and clears the link so it works only once.
        protected void btnSetPassword_Click(object sender, EventArgs e)
        {
            // Passwords are taken exactly as typed, so they are never trimmed.
            string next = txtResetNew.Text;
            bool ok = true;
            string problem = AuthService.PasswordProblem(next);
            if (problem != null)
                ok = ShowError(lblResetNewError, problem);
            if (txtResetConfirm.Text != next)
                ok = ShowError(lblResetConfirmError, "The two new passwords don't match.");
            if (!ok)
                return;

            if (!AuthService.CompleteOwnerReset(Request.QueryString["token"], next))
            {
                // The link ran out or was used while the page was open.
                pnlReset.Visible = false;
                pnlExpired.Visible = true;
                return;
            }

            Response.Redirect("~/Admin/Login.aspx?reset=done", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // Always returns false, so a failed check reads as ok = ShowError(...).
        private static bool ShowError(Label label, string text)
        {
            label.Text = text;
            label.Visible = true;
            return false;
        }
    }
}

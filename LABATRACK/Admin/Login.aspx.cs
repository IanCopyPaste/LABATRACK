using System;
using System.Web.UI.WebControls;
using LABATRACK.Helpers;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    public partial class Login : BasePage
    {
        // DRAFT: the one reset link that "works", so the design can be clicked through. Any other
        // token shows the expired state. Phase 5 checks the token against the owner's Users row.
        private const string DraftToken = "draft";

        // Same rule as the Settings page and a new staff account.
        private const int MinPasswordLength = 8;

        protected void Page_Load(object sender, EventArgs e)
        {
            // One card, four states, picked by the address: sign in, ask for a link (?forgot=1),
            // choose a new password (?token=...), or a link that no longer works.
            string token = Request.QueryString["token"];
            bool forgot = Request.QueryString["forgot"] == "1";

            pnlSignIn.Visible = token == null && !forgot;
            pnlForgot.Visible = token == null && forgot;
            pnlReset.Visible = token == DraftToken;
            pnlExpired.Visible = token != null && token != DraftToken;
            pnlResetDone.Visible = !IsPostBack && Request.QueryString["reset"] == "done";

            // Enter presses the button of the state on screen.
            if (pnlForgot.Visible) form1.DefaultButton = btnSendReset.ID;
            if (pnlReset.Visible) form1.DefaultButton = btnSetPassword.ID;
        }

        protected void btnSignIn_Click(object sender, EventArgs e)
        {
            // DRAFT: no password check yet, so the design flow can be clicked through.
            // Phase 2 replaces this with AuthService, and on failure shows pnlError
            // with the single message "Invalid username or password".
            string username = txtUsername.Text.Trim().ToLowerInvariant();
            SampleData.SignIn(Session, username == "" ? "owner" : username, true);
            Response.Redirect("~/Admin/Dashboard.aspx");
        }

        // DRAFT: nothing is sent yet. Phase 5: AuthService looks the username up, and only when it is
        // an active Owner account with a recovery email does it make a reset link: 32 random bytes,
        // of which only a SHA-256 hash is stored on the Users row with an expiry 30 minutes ahead
        // (a newer request replaces an older link). EmailService sends the link through SMTP.
        // For a staff account, an unknown username or no email, nothing is sent, and the page says
        // the same thing either way, so it never shows which accounts exist.
        protected void btnSendReset_Click(object sender, EventArgs e)
        {
            if (txtResetUsername.Text.Trim() == "")
            {
                ShowError(lblResetUsernameError, "Enter your username.");
                return;
            }
            pnlForgotForm.Visible = false;
            pnlSent.Visible = true;
        }

        // DRAFT: nothing is saved yet. Phase 5 checks the token again on this click (the hash matches,
        // not expired), saves the new password as a new hash with a new salt, clears the token so the
        // link works only once, and clears the failed-login count and any lock on the account.
        protected void btnSetPassword_Click(object sender, EventArgs e)
        {
            // Passwords are taken exactly as typed, so they are never trimmed.
            string next = txtResetNew.Text;
            bool ok = true;
            if (next.Length < MinPasswordLength)
                ok = ShowError(lblResetNewError, "Use at least " + MinPasswordLength + " characters.");
            if (txtResetConfirm.Text != next)
                ok = ShowError(lblResetConfirmError, "The two new passwords don't match.");
            if (!ok)
                return;

            Response.Redirect("~/Admin/Login.aspx?reset=done");
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

using System;
using System.Web.UI.WebControls;
using LABATRACK.Helpers;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    // DRAFT: design only. The page shows sample data until its phase is built.
    public partial class Settings : BasePage
    {
        protected override string ActiveNav
        {
            get { return "Settings"; }
        }

        // Same rule as a new staff account's temporary password on the Staff page.
        private const int MinPasswordLength = 8;

        // DRAFT: the name comes from the draft sign-in until Phase 2 adds AuthService.
        protected string SignedInUser { get { return SampleData.Username(Session); } }

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        // DRAFT: the checks below are the real rules, but nothing is saved and the current password
        // cannot be checked yet, because there is no Users table. Phase 2 hands the three values to
        // AuthService, which checks the current password against the stored PBKDF2 hash (a wrong one
        // shows "Current password is incorrect." and counts as a failed login, so the five-try lock
        // still applies), then saves the new password as a new hash with a new salt through a stored
        // procedure. The owner stays signed in.
        protected void btnChangePassword_Click(object sender, EventArgs e)
        {
            string current = txtCurrentPassword.Text;
            string next = txtNewPassword.Text;
            string confirm = txtConfirmPassword.Text;

            // Passwords are taken exactly as typed, spaces included, so they are never trimmed.
            bool ok = true;
            if (current == "")
                ok = ShowError(lblCurrentError, "Enter your current password.");
            if (next.Length < MinPasswordLength)
                ok = ShowError(lblNewError, "Use at least " + MinPasswordLength + " characters.");
            else if (next == current)
                ok = ShowError(lblNewError, "Choose a password different from the current one.");
            if (confirm != next)
                ok = ShowError(lblConfirmError, "The two new passwords don't match.");

            // A password box is always sent back empty, so there is nothing to clear afterwards.
            if (!ok)
                return;

            litPasswordNotice.Text = "<div class=\"notice notice-info\">" + Icons.Get("circle-check", "ico-sm")
                                   + "<span>Password changed. Use the new one the next time you sign in. "
                                   + "<span class=\"muted\">(Draft: nothing is saved yet.)</span></span></div>";
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

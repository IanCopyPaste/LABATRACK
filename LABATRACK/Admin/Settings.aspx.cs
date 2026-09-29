using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.DataAccess;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Admin
{
    // The owner's settings: shop details for the slip, job timing, the owner's own password, and
    // the recovery email for "Forgot password?". Each card has its own Save button (a plain submit
    // button named action=shop, timing or recovery); the password card has its own server button.
    public partial class Settings : AdminBasePage
    {
        protected override string ActiveNav
        {
            get { return "Settings"; }
        }

        protected string SignedInUser { get { return CurrentUser.Username; } }
        protected bool EmailReady { get { return EmailService.IsConfigured; } }
        protected string ClaimExample { get; private set; }

        private ShopSettings settings;

        protected void Page_Load(object sender, EventArgs e)
        {
            settings = SettingsRepository.Get();
            ClaimExample = "LT-" + DateTime.Now.ToString("yyMMdd", CultureInfo.InvariantCulture) + "-003";

            if (!IsPostBack)
            {
                txtShopName.Text = settings.ShopName;
                txtSlipHeader.Text = settings.SlipHeader;
                txtTurnaroundHours.Text = settings.DefaultTurnaroundHours.ToString(CultureInfo.InvariantCulture);
                txtUnclaimedDays.Text = settings.UnclaimedThresholdDays.ToString(CultureInfo.InvariantCulture);
                txtRecoveryEmail.Text = CurrentUser.RecoveryEmail ?? "";
                ShowSavedNotice();
                return;
            }

            switch (Request.Form["action"])
            {
                case "shop": SaveShop(); break;
                case "timing": SaveTiming(); break;
                case "recovery": SaveRecovery(); break;
            }
        }

        private void SaveShop()
        {
            string name = txtShopName.Text.Trim();
            string header = txtSlipHeader.Text.Trim();
            bool ok = true;
            if (name == "" || name.Length > 100)
                ok = ShowError(lblShopNameError, "Enter the shop name (up to 100 characters).");
            if (header.Length > 400)
                ok = ShowError(lblSlipHeaderError, "Keep the slip header to 400 characters.");
            if (!ok) return;

            SettingsRepository.Set(ShopSettings.ShopNameKey, name);
            SettingsRepository.Set(ShopSettings.SlipHeaderKey, header);
            Saved("shop");
        }

        private void SaveTiming()
        {
            int hours, days;
            bool ok = true;
            if (!int.TryParse(txtTurnaroundHours.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out hours) || hours < 1 || hours > 240)
                ok = ShowError(lblTurnaroundError, "Enter whole hours from 1 to 240.");
            if (!int.TryParse(txtUnclaimedDays.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out days) || days < 1 || days > 90)
                ok = ShowError(lblUnclaimedError, "Enter whole days from 1 to 90.");
            string rework = PostedRework();
            if (rework == "")
                ok = ShowError(lblReworkError, "Tick at least one stage.");
            if (!ok) return;

            SettingsRepository.Set(ShopSettings.DefaultTurnaroundHoursKey, hours);
            SettingsRepository.Set(ShopSettings.UnclaimedThresholdDaysKey, days);
            SettingsRepository.Set(ShopSettings.ReworkStagesKey, rework);
            Saved("timing");
        }

        private void SaveRecovery()
        {
            try
            {
                AuthService.SetRecoveryEmail(CurrentUser.UserId, txtRecoveryEmail.Text);
                Saved("recovery");
            }
            catch (BusinessRuleException ex)
            {
                ShowError(lblRecoveryError, ex.Message);
            }
        }

        // The owner's own password. AuthService checks the current password against the stored
        // hash (a wrong one counts as a failed sign-in, so the five-try lock still applies), then
        // saves the new one as a new hash with a new salt. The owner stays signed in.
        protected void btnChangePassword_Click(object sender, EventArgs e)
        {
            string current = txtCurrentPassword.Text;
            string next = txtNewPassword.Text;
            string confirm = txtConfirmPassword.Text;

            // Passwords are taken exactly as typed, spaces included, so they are never trimmed.
            bool ok = true;
            if (current == "")
                ok = ShowError(lblCurrentError, "Enter your current password.");
            string problem = AuthService.PasswordProblem(next);
            if (problem != null)
                ok = ShowError(lblNewError, problem);
            else if (next == current)
                ok = ShowError(lblNewError, "Choose a password different from the current one.");
            if (confirm != next)
                ok = ShowError(lblConfirmError, "The two new passwords don't match.");

            // A password box is always sent back empty, so there is nothing to clear afterwards.
            if (!ok)
                return;

            try
            {
                AuthService.ChangeOwnPassword(CurrentUser.UserId, current, next);
            }
            catch (BusinessRuleException ex)
            {
                ShowError(lblCurrentError, ex.Message);
                return;
            }

            litPasswordNotice.Text = "<div class=\"notice notice-info\">" + Icons.Get("circle-check", "ico-sm")
                                   + "<span>Password changed. Use the new one the next time you sign in.</span></div>";
        }

        // ------------------------------------------------------------ Pieces

        /// <summary>The rework boxes that were ticked, as letters in the fixed order: "WDF", "WD", ...</summary>
        private string PostedRework()
        {
            string[] ticked = Request.Form.GetValues("rework") ?? new string[0];
            return string.Concat(new[] { "W", "D", "F" }.Where(stage => ticked.Contains(stage)));
        }

        /// <summary>' checked="checked"' for a rework box that is on: as saved, or as just posted.</summary>
        protected string ReworkChecked(char stage)
        {
            string code = IsPostBack && Request.Form["action"] == "timing" ? PostedRework() : settings.ReworkStages;
            return code.IndexOf(stage) >= 0 ? " checked=\"checked\"" : "";
        }

        private void Saved(string what)
        {
            Response.Redirect("Settings.aspx?saved=" + what, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void ShowSavedNotice()
        {
            string text;
            switch (Request.QueryString["saved"])
            {
                case "shop": text = "Shop details saved. They print on the next slip."; break;
                case "timing": text = "Job timing saved. It applies to new job orders."; break;
                case "recovery": text = "Recovery email saved."; break;
                default: return;
            }
            litNotice.Text = "<div class=\"notice notice-info\">" + Icons.Get("circle-check", "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        // Always returns false, so a failed check reads as ok = ShowError(...).
        private static bool ShowError(Label label, string text)
        {
            label.Text = HttpUtility.HtmlEncode(text);
            label.Visible = true;
            return false;
        }
    }
}

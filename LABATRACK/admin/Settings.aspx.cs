using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.db_conn;
using LABATRACK.tools;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.admin
{
    public partial class Settings : AdminBasePage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected override string ActiveNav
        {
            get { return "Settings"; }
        }

        protected string SignedInUser { get { return CurrentUser.Username; } }
        protected bool EmailReady { get { return Email.IsConfigured; } }
        protected string ClaimExample { get; private set; }

        private ShopSettings settings = new ShopSettings();

        protected void Page_Load(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string key = (string)r["SettingKey"];
                        string value = r["SettingValue"] as string;
                        if (value == null) continue;

                        int number;
                        if (key == ShopSettings.ShopNameKey)
                            settings.ShopName = value;
                        else if (key == ShopSettings.SlipHeaderKey)
                            settings.SlipHeader = value;
                        else if (key == ShopSettings.ReworkStagesKey)
                            settings.ReworkStages = value;
                        else if (key == ShopSettings.DefaultTurnaroundHoursKey && int.TryParse(value, NumberStyles.Integer, Inv, out number))
                            settings.DefaultTurnaroundHours = number;
                        else if (key == ShopSettings.UnclaimedThresholdDaysKey && int.TryParse(value, NumberStyles.Integer, Inv, out number))
                            settings.UnclaimedThresholdDays = number;
                    }
                }
            }

            ClaimExample = "LT-" + DateTime.Now.ToString("yyMMdd", Inv) + "-003";

            if (!IsPostBack)
            {
                txtShopName.Text = settings.ShopName;
                txtSlipHeader.Text = settings.SlipHeader;
                txtTurnaroundHours.Text = settings.DefaultTurnaroundHours.ToString(Inv);
                txtUnclaimedDays.Text = settings.UnclaimedThresholdDays.ToString(Inv);
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
            {
                ShowError(lblShopNameError, "Enter the shop name (up to 100 characters).");
                ok = false;
            }
            if (header.Length > 400)
            {
                ShowError(lblSlipHeaderError, "Keep the slip header to 400 characters.");
                ok = false;
            }
            if (!ok) return;

            SaveSetting(ShopSettings.ShopNameKey, name);
            SaveSetting(ShopSettings.SlipHeaderKey, header);
            Saved("shop");
        }

        private void SaveTiming()
        {
            int hours, days;
            bool ok = true;

            if (!int.TryParse(txtTurnaroundHours.Text.Trim(), NumberStyles.None, Inv, out hours) || hours < 1 || hours > 240)
            {
                ShowError(lblTurnaroundError, "Enter whole hours from 1 to 240.");
                ok = false;
            }
            if (!int.TryParse(txtUnclaimedDays.Text.Trim(), NumberStyles.None, Inv, out days) || days < 1 || days > 90)
            {
                ShowError(lblUnclaimedError, "Enter whole days from 1 to 90.");
                ok = false;
            }
            string rework = PostedRework();
            if (rework == "")
            {
                ShowError(lblReworkError, "Tick at least one stage.");
                ok = false;
            }
            if (!ok) return;

            SaveSetting(ShopSettings.DefaultTurnaroundHoursKey, hours.ToString(Inv));
            SaveSetting(ShopSettings.UnclaimedThresholdDaysKey, days.ToString(Inv));
            SaveSetting(ShopSettings.ReworkStagesKey, rework);
            Saved("timing");
        }

        private void SaveRecovery()
        {
            string email = txtRecoveryEmail.Text.Trim();
            if (email != "" && !Email.IsValidAddress(email))
            {
                ShowError(lblRecoveryError, "That does not look like an email address.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_SetRecoveryEmail", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = CurrentUser.UserId;
                cmd.Parameters.Add("@RecoveryEmail", SqlDbType.NVarChar, 254).Value = email == "" ? (object)DBNull.Value : email;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            Saved("recovery");
        }

        protected void btnChangePassword_Click(object sender, EventArgs e)
        {
            string current = txtCurrentPassword.Text;
            string next = txtNewPassword.Text;
            string confirm = txtConfirmPassword.Text;
            bool ok = true;

            if (current == "")
            {
                ShowError(lblCurrentError, "Enter your current password.");
                ok = false;
            }
            if (next.Length < 8)
            {
                ShowError(lblNewError, "Use at least 8 characters.");
                ok = false;
            }
            else if (next.Length > 100)
            {
                ShowError(lblNewError, "Use at most 100 characters.");
                ok = false;
            }
            else if (next == current)
            {
                ShowError(lblNewError, "Choose a password different from the current one.");
                ok = false;
            }
            if (confirm != next)
            {
                ShowError(lblConfirmError, "The two new passwords don't match.");
                ok = false;
            }
            if (!ok) return;

            byte[] oldSalt = null;
            byte[] oldHash = null;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = CurrentUser.UserId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        oldSalt = (byte[])r["PasswordSalt"];
                        oldHash = (byte[])r["PasswordHash"];
                    }
                }
            }

            if (oldHash == null)
            {
                ShowError(lblCurrentError, "Your account could not be found. Sign in again.");
                return;
            }

            if (!PasswordHasher.Verify(current, oldSalt, oldHash))
            {
                ShowError(lblCurrentError, "Current password is incorrect.");
                return;
            }

            byte[] salt = PasswordHasher.NewSalt();
            byte[] hash = PasswordHasher.Hash(next, salt);

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_SetPassword", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = CurrentUser.UserId;
                cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = hash;
                cmd.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = salt;

                conn.Open();
                cmd.ExecuteScalar();
            }

            litPasswordNotice.Text = "<div class=\"notice notice-info\">" + Icons.Get("circle-check", "ico-sm")
                                   + "<span>Password changed. Use the new one the next time you sign in.</span></div>";
        }

        private void SaveSetting(string key, string value)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_Set", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@SettingKey", SqlDbType.NVarChar, 50).Value = key;
                cmd.Parameters.Add("@SettingValue", SqlDbType.NVarChar, 400).Value = value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private string PostedRework()
        {
            string[] ticked = Request.Form.GetValues("rework") ?? new string[0];
            return string.Concat(new[] { "W", "D", "F" }.Where(stage => ticked.Contains(stage)));
        }

        protected string ReworkChecked(char stage)
        {
            string code;
            if (IsPostBack && Request.Form["action"] == "timing")
                code = PostedRework();
            else
                code = settings.ReworkStages;
            if (code.IndexOf(stage) >= 0)
                return " checked=\"checked\"";
            else
                return "";
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

        private void ShowError(Label label, string text)
        {
            label.Text = HttpUtility.HtmlEncode(text);
            label.Visible = true;
        }
    }
}

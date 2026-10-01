using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using LABATRACK.db_conn;
using LABATRACK.security;

namespace LABATRACK.admin
{
    public partial class Login : BasePage
    {
        protected override bool AllowAnonymous
        {
            get { return true; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && CurrentUser != null)
            {
                if (CurrentUser.IsOwner)
                    Response.Redirect("~/admin/Dashboard.aspx");

                litSignedInAs.Text = HttpUtility.HtmlEncode(CurrentUser.Username);
                pnlStaffSignedIn.Visible = true;
            }
        }

        protected void btnSignIn_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim().ToLowerInvariant();
            bool found = false;
            bool isActive = false;
            string role = "";
            byte[] salt = null;
            byte[] hash = null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetByUsername", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        found = true;
                        isActive = (bool)r["IsActive"];
                        role = (string)r["Role"];
                        salt = (byte[])r["PasswordSalt"];
                        hash = (byte[])r["PasswordHash"];
                    }
                }
            }

            if (found == false || isActive == false || role != "Owner" || PasswordHasher.Verify(txtPassword.Text, salt, hash) == false)
            {
                lblError.Text = "Invalid username or password";
                pnlError.Visible = true;
                return;
            }

            Session["Username"] = username;
            Session["Role"] = role;

            Response.Redirect("~/admin/Dashboard.aspx");
        }

        protected void lnkForgot_Click(object sender, EventArgs e)
        {
            pnlSignIn.Visible = false;
            pnlForgot.Visible = true;
        }

        protected void lnkBack_Click(object sender, EventArgs e)
        {
            pnlForgot.Visible = false;
            pnlSignIn.Visible = true;
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            string username = txtResetUsername.Text.Trim().ToLowerInvariant();
            string email = txtResetEmail.Text.Trim();
            string newPassword = txtResetNew.Text;

            if (newPassword.Length < 8)
            {
                lblResetError.Text = "Use at least 8 characters for the new password.";
                pnlResetError.Visible = true;
                return;
            }

            if (txtResetConfirm.Text != newPassword)
            {
                lblResetError.Text = "The two new passwords don't match.";
                pnlResetError.Visible = true;
                return;
            }

            bool found = false;
            int userId = 0;
            bool isActive = false;
            string role = "";
            string recoveryEmail = "";

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetByUsername", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        found = true;
                        userId = (int)r["UserId"];
                        isActive = (bool)r["IsActive"];
                        role = (string)r["Role"];
                        if (r["RecoveryEmail"] != DBNull.Value)
                            recoveryEmail = (string)r["RecoveryEmail"];
                    }
                }
            }

            if (found == false || isActive == false || role != "Owner" || recoveryEmail == "" || recoveryEmail.ToLower() != email.ToLower())
            {
                lblResetError.Text = "Username or recovery email is wrong.";
                pnlResetError.Visible = true;
                return;
            }

            byte[] salt = PasswordHasher.NewSalt();
            byte[] hash = PasswordHasher.Hash(newPassword, salt);

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_SetPassword", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = hash;
                cmd.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = salt;

                conn.Open();
                cmd.ExecuteScalar();
            }

            pnlForgot.Visible = false;
            pnlSignIn.Visible = true;
            pnlResetDone.Visible = true;
        }
    }
}

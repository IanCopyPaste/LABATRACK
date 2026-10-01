using System;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.db_conn;
using LABATRACK.security;

namespace LABATRACK
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
                Response.Redirect("~/Default.aspx");
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

            if (found == false || isActive == false || role != "Staff" || PasswordHasher.Verify(txtPassword.Text, salt, hash) == false)
            {
                lblError.Text = "Invalid username or password";
                pnlError.Visible = true;
                return;
            }

            Session["Username"] = username;
            Session["Role"] = role;

            Response.Redirect("~/pages/shared/Dashboard.aspx");
        }
    }
}

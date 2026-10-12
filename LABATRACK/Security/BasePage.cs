using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using LABATRACK.db_conn;
using LABATRACK.tools;
using LABATRACK.models;

namespace LABATRACK.security
{
    public class BasePage : Page
    {
        protected virtual string ActiveNav
        {
            get { return ""; }
        }

        protected virtual string PageStyleSheet
        {
            get { return ""; }
        }

        protected virtual bool AllowAnonymous
        {
            get { return false; }
        }

        private UserAccount currentUser;
        private bool currentUserLoaded;

        protected UserAccount CurrentUser
        {
            get
            {
                if (currentUserLoaded)
                    return currentUser;

                currentUserLoaded = true;
                string username = Session["Username"] as string;
                if (username == null)
                    return null;

                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetByUsername", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;

                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read() && (bool)r["IsActive"])
                        {
                            currentUser = new UserAccount();
                            currentUser.UserId = (int)r["UserId"];
                            currentUser.Username = (string)r["Username"];
                            currentUser.Role = (string)r["Role"];
                            currentUser.IsActive = true;
                            currentUser.RecoveryEmail = r["RecoveryEmail"] as string;
                        }
                    }
                }
                return currentUser;
            }
        }

        protected bool IsOwner
        {
            get { return CurrentUser != null && CurrentUser.IsOwner; }
        }

        protected override void OnInit(EventArgs e)
        {
            CheckAccess();

            if (CurrentUser != null)
                ViewStateUserKey = CurrentUser.Username;

            base.OnInit(e);
        }

        protected virtual void CheckAccess()
        {
            if (AllowAnonymous == false && CurrentUser == null)
                RedirectToLogin("~/Login.aspx");
        }

        protected void RedirectToLogin(string loginPage)
        {
            if (Session["Username"] != null && CurrentUser == null)
                Session.Abandon();
            Response.Redirect(loginPage);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Header != null)
                Header.Controls.AddAt(0, new LiteralControl(SiteStyle.StyleTag(PageStyleSheet)));

            Literal nav = FindControl("litNav") as Literal;
            if (nav != null && CurrentUser != null)
                nav.Text = NavBar.Render(ActiveNav, CurrentUser);
        }
    }
}

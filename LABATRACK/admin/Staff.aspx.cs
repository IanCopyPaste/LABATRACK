using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using LABATRACK.db_conn;
using LABATRACK.tools;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.admin
{
    public partial class Staff : AdminBasePage
    {
        protected override string ActiveNav
        {
            get { return "Staff"; }
        }

        protected override string PageStyleSheet
        {
            get { return "Staff.css"; }
        }

        protected int AccountCount { get; private set; }
        protected int CanSignInCount { get; private set; }

        protected bool DialogOpen { get; private set; }
        protected bool DialogIsManage { get; private set; }
        protected bool DialogHasHistory { get; private set; }
        protected string DialogUserId { get; private set; }
        protected string DialogTitle { get; private set; }
        protected string DialogSub { get; private set; }
        protected string DialogUsername { get; private set; }
        protected bool DialogActive { get; private set; }
        protected string DialogError { get; private set; }
        protected string UsernameError { get; private set; }
        protected string PasswordError { get; private set; }
        protected string DeleteConfirmText { get; private set; }

        private List<UserAccount> users = new List<UserAccount>();
        private string justChanged;

        protected void Page_Load(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        UserAccount user = new UserAccount();
                        user.UserId = (int)r["UserId"];
                        user.Username = (string)r["Username"];
                        user.Role = (string)r["Role"];
                        user.IsActive = (bool)r["IsActive"];
                        user.HasHistory = Convert.ToBoolean(r["HasHistory"]);
                        users.Add(user);
                    }
                }
            }

            DialogTitle = "Add a staff account";
            DialogSub = "New accounts get the Staff role and sign in at the counter login.";
            DialogActive = true;
            DialogUserId = "";

            if (IsPostBack)
                HandlePost();
            else
                ShowDoneNotice();

            AccountCount = users.Count;
            CanSignInCount = users.Count(u => u.IsActive);
            rptUsers.DataSource = users;
            rptUsers.DataBind();
        }

        private void HandlePost()
        {
            string action = Request.Form["action"];
            int userId;
            bool manage = int.TryParse(Request.Form["udId"], out userId);
            string username = Request.Form["udUsername"] ?? "";
            string password = Request.Form["udPassword"] ?? "";
            bool active = Request.Form["udActive"] == "1";

            if (action == "save-user")
            {
                SaveUser(manage, userId, username, password, active);
            }
            else if (action == "delete-user" && manage)
            {
                DeleteUser(userId, username, active);
            }
        }

        private void SaveUser(bool manage, int userId, string username, string password, bool active)
        {
            string name = username.Trim().ToLowerInvariant();

            if (name.Length < 3 || name.Length > 50)
            {
                UsernameError = "Use 3 to 50 characters.";
            }
            else if (name.Any(ch => !((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '.' || ch == '_' || ch == '-')))
            {
                UsernameError = "Use letters, numbers, dots, dashes or underscores only.";
            }
            else
            {
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetByUsername", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = name;

                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            int takenBy = (int)r["UserId"];
                            if (manage == false || takenBy != userId)
                                UsernameError = "That username is already taken.";
                        }
                    }
                }
            }

            if (manage == false || password != "")
            {
                if (password.Length < 8)
                    PasswordError = "Use at least 8 characters.";
                else if (password.Length > 100)
                    PasswordError = "Use at most 100 characters.";
            }

            if (UsernameError == null && PasswordError == null)
            {
                try
                {
                    if (manage)
                    {
                        string role = null;
                        using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                        using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetById", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                            conn.Open();
                            using (SqlDataReader r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                    role = (string)r["Role"];
                            }
                        }

                        if (role != "Staff")
                        {
                            DialogError = "Only staff accounts are managed here. Change your own account on Settings.";
                        }
                        else
                        {
                            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_UpdateStaff", conn))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = name;
                                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = active;

                                conn.Open();
                                cmd.ExecuteScalar();
                            }

                            if (password != "")
                            {
                                byte[] salt = PasswordHasher.NewSalt();
                                byte[] hash = PasswordHasher.Hash(password, salt);

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
                            }

                            Done("saved", name);
                            return;
                        }
                    }
                    else
                    {
                        byte[] salt = PasswordHasher.NewSalt();
                        byte[] hash = PasswordHasher.Hash(password, salt);

                        using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                        using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_Insert", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = name;
                            cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = hash;
                            cmd.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = salt;
                            cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 10).Value = "Staff";
                            cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = active;

                            conn.Open();
                            cmd.ExecuteScalar();
                        }

                        Done("created", name);
                        return;
                    }
                }
                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                {
                    DialogError = "That username was just taken.";
                }
            }

            ReopenDialog(manage ? userId : (int?)null, username, active);
        }

        private void DeleteUser(int userId, string username, bool active)
        {
            string role = null;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        role = (string)r["Role"];
                }
            }

            UserAccount user = users.FirstOrDefault(u => u.UserId == userId);
            string shownName;
            if (user != null)
                shownName = user.Username;
            else
                shownName = username;

            if (role != "Staff")
            {
                DialogError = "Only a staff account can be deleted.";
            }
            else
            {
                int rowsAffected;
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_DeleteUnused", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    conn.Open();
                    rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (rowsAffected == 1)
                {
                    Done("deleted", shownName);
                    return;
                }

                DialogError = shownName + " has jobs, payments or history on record, so the account can't be deleted. "
                            + "Switch it to Inactive instead: it can't sign in, and its name stays on those records.";
            }

            ReopenDialog(userId, username, active);
        }

        private void ReopenDialog(int? userId, string typedUsername, bool active)
        {
            DialogOpen = true;
            DialogUsername = typedUsername;
            DialogActive = active;
            UserAccount user = userId.HasValue ? users.FirstOrDefault(u => u.UserId == userId.Value) : null;
            if (user == null) return;

            DialogIsManage = true;
            DialogHasHistory = user.HasHistory;
            DialogUserId = user.UserId.ToString();
            DialogTitle = "Manage " + user.Username;
            DialogSub = StatusNote(user);
            DeleteConfirmText = "Delete " + user.Username + " for good? It has no jobs, payments or history, so nothing else changes. This can't be undone.";
        }

        private void Done(string what, string username)
        {
            Response.Redirect("Staff.aspx?done=" + what + "&u=" + HttpUtility.UrlEncode(username.Trim().ToLowerInvariant()), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void ShowDoneNotice()
        {
            string u = Request.QueryString["u"];
            if (string.IsNullOrEmpty(u)) return;
            justChanged = u;
            string text;
            switch (Request.QueryString["done"])
            {
                case "created": text = "Created " + u + ". Share the password with them in person."; break;
                case "saved": text = "Saved the changes to " + u + "."; break;
                case "deleted": text = "Deleted " + u + "."; break;
                default: return;
            }
            litNotice.Text = "<div class=\"notice notice-info\">" + Icons.Get("circle-check", "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        protected string RowCss(UserAccount u)
        {
            if (string.Equals(u.Username, justChanged, StringComparison.OrdinalIgnoreCase))
                return "is-changed";
            else
                return "";
        }

        protected static string StatusNote(UserAccount u)
        {
            if (!u.IsActive) return "Inactive: it can't sign in.";
            return "Active: it can sign in at the counter login.";
        }

        protected static string StatusPill(UserAccount u)
        {
            if (!u.IsActive) return "<span class=\"pill st-claimed\">Inactive</span>";
            return "<span class=\"pill pill-ok\">Active</span>";
        }
    }
}

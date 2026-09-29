using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LABATRACK.DataAccess;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Admin
{
    // Staff accounts (owner only). "Add staff account" and each row's "Manage" open one window
    // (Staff.js); it posts udId (empty when adding), udUsername, udPassword and udActive, with
    // action=save-user or action=delete-user. The rules are in AuthService. A save that goes through
    // redirects back here with ?done=...&u=<username> (post, then redirect), so a refresh never saves
    // twice; a refused one draws the window open again with what was typed and what is wrong.
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

        // The window's state when the server draws it (open again after a refused save).
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

        private List<UserAccount> users;
        private string justChanged;

        protected void Page_Load(object sender, EventArgs e)
        {
            users = UserRepository.GetAll();

            // The window starts as "add"; Staff.js changes it for Manage.
            DialogTitle = "Add a staff account";
            DialogSub = "New accounts get the Staff role and sign in at the counter login.";
            DialogActive = true;
            DialogUserId = "";

            if (IsPostBack)
                HandlePost();
            else
                ShowDoneNotice();

            DateTime now = DateTime.Now;
            AccountCount = users.Count;
            CanSignInCount = users.Count(u => u.IsActive && !u.IsLockedAt(now));
            rptUsers.DataSource = users;
            rptUsers.DataBind();
        }

        private void HandlePost()
        {
            string action = Request.Form["action"];
            int userId;
            bool manage = int.TryParse(Request.Form["udId"], out userId);
            string username = Request.Form["udUsername"] ?? "";
            string password = Request.Form["udPassword"] ?? "";   // taken exactly as typed, never trimmed
            bool active = Request.Form["udActive"] == "1";

            try
            {
                if (action == "save-user")
                {
                    // Each box's own problem first, so it shows under that box.
                    UsernameError = AuthService.UsernameProblem(username, manage ? userId : (int?)null);
                    if (!manage || password != "")
                        PasswordError = AuthService.PasswordProblem(password);
                    if (UsernameError == null && PasswordError == null)
                    {
                        if (manage)
                        {
                            AuthService.UpdateStaff(userId, username, password, active);
                            Done("saved", username);
                        }
                        else
                        {
                            AuthService.CreateStaff(username, password, active);
                            Done("created", username);
                        }
                        return;
                    }
                }
                else if (action == "delete-user" && manage)
                {
                    UserAccount user = users.FirstOrDefault(u => u.UserId == userId);
                    AuthService.DeleteStaff(userId);
                    Done("deleted", user != null ? user.Username : username);
                    return;
                }
                else
                {
                    return;
                }
            }
            catch (BusinessRuleException ex)
            {
                DialogError = ex.Message;
            }

            ReopenDialog(manage ? userId : (int?)null, username, active);
        }

        /// <summary>Draws the window open again, as it was when it was sent, with what went wrong.</summary>
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
            DeleteConfirmText = DeleteText(user.Username);
        }

        private void Done(string what, string username)
        {
            Response.Redirect("Staff.aspx?done=" + what + "&u=" + HttpUtility.UrlEncode(AuthService.NormalizeUsername(username)), false);
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

        private static string DeleteText(string username)
        {
            return "Delete " + username + " for good? It has no jobs, payments or history, so nothing else changes. This can't be undone.";
        }

        // ---- Used by the markup ----

        /// <summary>The account just saved is washed in the accent for a moment.</summary>
        protected string RowCss(UserAccount u)
        {
            return string.Equals(u.Username, justChanged, StringComparison.OrdinalIgnoreCase) ? "is-changed" : "";
        }

        /// <summary>One line about the account, shown under the Manage window's title.</summary>
        protected static string StatusNote(UserAccount u)
        {
            if (!u.IsActive) return "Inactive: it can't sign in.";
            if (u.IsLockedAt(DateTime.Now))
                return "Locked until " + u.LockedUntil.Value.ToString("h:mm tt") + " after five wrong passwords. A new password unlocks it now.";
            return "Active: it can sign in at the counter login.";
        }

        protected static string StatusPill(UserAccount u)
        {
            if (!u.IsActive) return "<span class=\"pill st-claimed\">Inactive</span>";
            if (u.IsLockedAt(DateTime.Now))
                return "<span class=\"pill pill-warn\">Locked until " + u.LockedUntil.Value.ToString("h:mm tt") + "</span>";
            return "<span class=\"pill pill-ok\">Active</span>";
        }
    }
}

using System.Text;
using System.Web;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// Builds the sidebar menu. Replaces a .master page: every page has an
    /// &lt;asp:Literal ID="litNav"&gt; and BasePage fills it with this HTML.
    /// There are two menus: the owner's (everything) and staff's (the counter screens).
    /// DRAFT: the role comes from the draft sign-in until Phase 2 adds AuthService.
    /// </summary>
    public static class NavBar
    {
        private class Item
        {
            public string Key, Text, Url, Icon;
            public bool Built;
            public Item(string key, string text, string url, string icon, bool built)
            {
                Key = key; Text = text; Url = url; Icon = icon; Built = built;
            }
        }

        private static readonly Item[] Overview =
        {
            new Item("Dashboard", "Dashboard", "~/Admin/Dashboard.aspx", "layout-dashboard", true),
        };

        // Counter screens the owner can also use, so no second staff account is needed.
        private static readonly Item[] Counter =
        {
            new Item("NewJob", "New job order", "~/Pages/Shared/NewJob.aspx", "plus", true),
            new Item("Board", "Job board", "~/Pages/Shared/Dashboard.aspx", "washing-machine", true),
            new Item("History", "History", "~/Pages/Shared/History.aspx", "history", true),
            new Item("Customers", "Customers", "~/Pages/Shared/Customers.aspx", "users", true),
        };

        private static readonly Item[] Management =
        {
            new Item("Staff", "Staff accounts", "~/Admin/Staff.aspx", "shield-check", true),
            new Item("Pricing", "Pricing", "~/Admin/Pricing.aspx", "tags", true),
            new Item("Reports", "Reports", "~/Admin/Reports.aspx", "chart-column", true),
            new Item("Settings", "Settings", "~/Admin/Settings.aspx", "settings", true),
        };

        // The staff menu: the same counter items as above, with the job board moved first as
        // their home. Built from Counter (declared above, so it already exists here) so an item
        // is only ever written once.
        private static readonly Item[] StaffCounter = { Counter[1], Counter[0], Counter[2], Counter[3] };

        public static string Render(string activeKey)
        {
            // DRAFT: the role comes from the draft sign-in in SampleData. Phase 2 reads the
            // signed-in account from AuthService instead. The menu only shows what a role may
            // open; the pages themselves are guarded on the server (AdminBasePage, Web.config).
            var session = HttpContext.Current == null ? null : HttpContext.Current.Session;
            bool owner = SampleData.IsOwner(session);
            string username = SampleData.Username(session);

            var sb = new StringBuilder();
            sb.Append("<aside class=\"sidebar\">");
            sb.Append("<div class=\"brand\"><div class=\"brand-mark\">").Append(Icons.Get("washing-machine"))
              .Append("</div><div><div class=\"brand-name\">LabaTrack</div><div class=\"brand-sub\">")
              .Append(owner ? "Owner console" : "Counter").Append("</div></div></div>");

            sb.Append("<nav class=\"nav\">");
            if (owner)
            {
                AppendSection(sb, "Overview", Overview, activeKey);
                AppendSection(sb, "Counter", Counter, activeKey);
                AppendSection(sb, "Management", Management, activeKey);
            }
            else
            {
                AppendSection(sb, "Counter", StaffCounter, activeKey);
            }
            sb.Append("</nav>");

            string initials = username.Length >= 2 ? username.Substring(0, 2).ToUpperInvariant() : username.ToUpperInvariant();
            sb.Append("<div class=\"sidebar-foot\"><div class=\"user-chip\">")
              .Append("<div class=\"avatar\">").Append(HttpUtility.HtmlEncode(initials)).Append("</div>")
              .Append("<div class=\"who\"><div>").Append(HttpUtility.HtmlEncode(username)).Append("</div><div class=\"small muted\">")
              .Append(owner ? "Owner" : "Staff").Append("</div></div>")
              .Append("<a class=\"icon-btn\" title=\"Sign out\" href=\"").Append(Url("~/Logout.aspx")).Append("\">")
              .Append(Icons.Get("log-out")).Append("</a></div>")
              .Append("<div class=\"draft-note\">Design draft &middot; sample data only</div>")
              .Append("</div>");

            sb.Append("</aside>");
            return sb.ToString();
        }

        private static void AppendSection(StringBuilder sb, string title, Item[] items, string activeKey)
        {
            sb.Append("<div class=\"nav-section\">").Append(HttpUtility.HtmlEncode(title)).Append("</div>");
            foreach (var item in items)
            {
                string text = HttpUtility.HtmlEncode(item.Text);
                if (!item.Built)
                {
                    // Not built yet: shown so the flow is visible, but not clickable.
                    sb.Append("<span class=\"nav-soon\">").Append(Icons.Get(item.Icon)).Append(text)
                      .Append("<span class=\"nav-tag\">Soon</span></span>");
                    continue;
                }
                string css = item.Key == activeKey ? " class=\"active\"" : "";
                sb.Append("<a").Append(css).Append(" href=\"").Append(Url(item.Url)).Append("\">")
                  .Append(Icons.Get(item.Icon)).Append(text).Append("</a>");
            }
        }

        private static string Url(string appRelative)
        {
            return HttpUtility.HtmlAttributeEncode(VirtualPathUtility.ToAbsolute(appRelative));
        }
    }
}

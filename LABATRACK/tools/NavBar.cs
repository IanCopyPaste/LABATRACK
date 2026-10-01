using System.Text;
using System.Web;
using LABATRACK.models;

namespace LABATRACK.tools
{
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
            new Item("Dashboard", "Dashboard", "~/admin/Dashboard.aspx", "layout-dashboard", true),
        };

        private static readonly Item[] Counter =
        {
            new Item("NewJob", "New job order", "~/pages/shared/NewJob.aspx", "plus", true),
            new Item("Board", "Job board", "~/pages/shared/Dashboard.aspx", "washing-machine", true),
            new Item("History", "History", "~/pages/shared/History.aspx", "history", true),
            new Item("Customers", "Customers", "~/pages/shared/Customers.aspx", "users", true),
        };

        private static readonly Item[] Management =
        {
            new Item("Staff", "Staff accounts", "~/admin/Staff.aspx", "shield-check", true),
            new Item("Pricing", "Pricing", "~/admin/Pricing.aspx", "tags", true),
            new Item("Reports", "Reports", "~/admin/Reports.aspx", "chart-column", true),
            new Item("Settings", "Settings", "~/admin/Settings.aspx", "settings", true),
        };

        private static readonly Item[] StaffCounter = { Counter[1], Counter[0], Counter[2], Counter[3] };

        public const string CollapsedCookie = "lt-sidebar";

        public static string Render(string activeKey, UserAccount user)
        {
            bool owner = user.IsOwner;
            string username = user.Username;
            HttpCookie cookie = HttpContext.Current == null ? null : HttpContext.Current.Request.Cookies[CollapsedCookie];
            bool collapsed = cookie != null && cookie.Value == "collapsed";

            var sb = new StringBuilder();
            sb.Append("<aside class=\"sidebar").Append(collapsed ? " is-collapsed" : "").Append("\" data-sidebar=\"true\">");
            sb.Append("<div class=\"brand\"><div class=\"brand-mark\">").Append(Icons.Get("washing-machine"))
              .Append("</div><div class=\"brand-text\"><div class=\"brand-name\">LabaTrack</div><div class=\"brand-sub\">")
              .Append(owner ? "Owner console" : "Counter").Append("</div></div>")
              .Append("<button type=\"button\" class=\"sidebar-toggle\" data-sidebar-toggle=\"true\" aria-expanded=\"")
              .Append(collapsed ? "false" : "true").Append("\" aria-label=\"").Append(collapsed ? "Expand the menu" : "Collapse the menu")
              .Append("\" title=\"").Append(collapsed ? "Expand the menu" : "Collapse the menu").Append("\">")
              .Append(Icons.Get("panel-left-close", "when-open")).Append(Icons.Get("panel-left-open", "when-collapsed"))
              .Append("</button></div>");

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

            string initials;
            if (username.Length >= 2)
                initials = username.Substring(0, 2).ToUpperInvariant();
            else
                initials = username.ToUpperInvariant();
            sb.Append("<div class=\"sidebar-foot\"><div class=\"user-chip\">")
              .Append("<div class=\"avatar\">").Append(HttpUtility.HtmlEncode(initials)).Append("</div>")
              .Append("<div class=\"who\"><div>").Append(HttpUtility.HtmlEncode(username)).Append("</div><div class=\"small muted\">")
              .Append(owner ? "Owner" : "Staff").Append("</div></div>")
              .Append("<a class=\"icon-btn\" title=\"Sign out\" href=\"").Append(Url("~/Logout.aspx")).Append("\">")
              .Append(Icons.Get("log-out")).Append("</a></div>")
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
                    sb.Append("<span class=\"nav-soon\">").Append(Icons.Get(item.Icon)).Append(text)
                      .Append("<span class=\"nav-tag\">Soon</span></span>");
                    continue;
                }
                string css;
                if (item.Key == activeKey)
                    css = " class=\"active\"";
                else
                    css = "";
                sb.Append("<a").Append(css).Append(" href=\"").Append(Url(item.Url)).Append("\" title=\"").Append(text).Append("\">")
                  .Append(Icons.Get(item.Icon)).Append("<span class=\"nav-label\">").Append(text).Append("</span></a>");
            }
        }

        private static string Url(string appRelative)
        {
            return HttpUtility.HtmlAttributeEncode(VirtualPathUtility.ToAbsolute(appRelative));
        }
    }
}

using System.Web;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// Builds the &lt;style&gt; block that loads the stylesheets for a page.
    /// The CSS itself lives in ~/Assets/css: Site.css for every page, and
    /// pages/&lt;PageName&gt;.css for rules only one page needs.
    /// It uses @import inside &lt;style&gt; because the project does not use &lt;link&gt; tags.
    /// BasePage puts the result at the top of each page's &lt;head&gt;.
    /// </summary>
    public static class SiteStyle
    {
        public static string StyleTag(string pageStyleSheet)
        {
            string tag = "<style>@import url('" + Url("~/Assets/css/Site.css") + "');";
            if (!string.IsNullOrEmpty(pageStyleSheet))
                tag += " @import url('" + Url("~/Assets/css/pages/" + pageStyleSheet) + "');";
            return tag + "</style>";
        }

        // The file names are fixed in code (never user input), so no encoding is needed.
        private static string Url(string appRelative)
        {
            return VirtualPathUtility.ToAbsolute(appRelative);
        }
    }
}

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
            // Versioned URLs, so an edited stylesheet is fetched again instead of served from cache.
            string tag = "<style>@import url('" + AssetUrl.Get("~/Assets/css/Site.css") + "');";
            if (!string.IsNullOrEmpty(pageStyleSheet))
                tag += " @import url('" + AssetUrl.Get("~/Assets/css/pages/" + pageStyleSheet) + "');";
            // The file names are fixed in code (never user input), so no encoding is needed.
            return tag + "</style>";
        }
    }
}

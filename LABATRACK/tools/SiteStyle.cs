using System.Web;

namespace LABATRACK.tools
{
    public static class SiteStyle
    {
        public static string StyleTag(string pageStyleSheet)
        {
            string tag = "<style>@import url('" + AssetUrl.Get("~/assets/css/Site.css") + "');";
            if (!string.IsNullOrEmpty(pageStyleSheet))
                tag += " @import url('" + AssetUrl.Get("~/assets/css/pages/" + pageStyleSheet) + "');";
            return tag + "</style>";
        }
    }
}

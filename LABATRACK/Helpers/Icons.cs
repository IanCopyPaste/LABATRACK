using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// Returns an icon from ~/Assets/icons as inline &lt;svg&gt; markup, so it takes the
    /// text colour around it. Icons are local files (Lucide, ISC licence); no CDN, no icon font.
    /// Usage in a page: &lt;%= Icons.Get("plus") %&gt;
    /// </summary>
    public static class Icons
    {
        private static readonly ConcurrentDictionary<string, string> Cache = new ConcurrentDictionary<string, string>();
        private static readonly Regex SafeName = new Regex("^[a-z0-9-]+$");

        public static string Get(string name, string extraClass = "")
        {
            // Only simple file names, so a name can never point outside the icons folder.
            if (!SafeName.IsMatch(name)) return "";

            string svg = Cache.GetOrAdd(name, Load);
            string cssClass = string.IsNullOrEmpty(extraClass) ? "ico" : "ico " + extraClass;
            return svg.Replace("class=\"ico\"", "class=\"" + cssClass + "\"");
        }

        private static string Load(string name)
        {
            string path = HttpContext.Current.Server.MapPath("~/Assets/icons/" + name + ".svg");
            if (!File.Exists(path)) return "";

            string svg = File.ReadAllText(path);
            svg = svg.Substring(svg.IndexOf("<svg"));             // drop the licence comment
            int tagEnd = svg.IndexOf('>');
            string openTag = svg.Substring(0, tagEnd);
            openTag = Regex.Replace(openTag, "\\s(class|width|height)=\"[^\"]*\"", "");  // size comes from CSS
            openTag += " class=\"ico\" aria-hidden=\"true\"";
            return Regex.Replace(openTag + svg.Substring(tagEnd), "\\s+", " ");
        }
    }
}

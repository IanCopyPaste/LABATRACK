using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace LABATRACK.tools
{
    public static class Icons
    {
        private static readonly ConcurrentDictionary<string, string> Cache = new ConcurrentDictionary<string, string>();
        private static readonly Regex SafeName = new Regex("^[a-z0-9-]+$");

        public static string Get(string name, string extraClass = "")
        {
            if (!SafeName.IsMatch(name)) return "";

            string svg = Cache.GetOrAdd(name, Load);
            string cssClass;
            if (string.IsNullOrEmpty(extraClass))
                cssClass = "ico";
            else
                cssClass = "ico " + extraClass;
            return svg.Replace("class=\"ico\"", "class=\"" + cssClass + "\"");
        }

        private static string Load(string name)
        {
            string path = HttpContext.Current.Server.MapPath("~/assets/icons/" + name + ".svg");
            if (!File.Exists(path)) return "";

            string svg = File.ReadAllText(path);
            svg = svg.Substring(svg.IndexOf("<svg"));
            int tagEnd = svg.IndexOf('>');
            string openTag = svg.Substring(0, tagEnd);
            openTag = Regex.Replace(openTag, "\\s(class|width|height)=\"[^\"]*\"", "");
            openTag += " class=\"ico\" aria-hidden=\"true\"";
            return Regex.Replace(openTag + svg.Substring(tagEnd), "\\s+", " ");
        }
    }
}

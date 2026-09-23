using System;
using System.Collections.Concurrent;
using System.IO;
using System.Web;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// Returns the URL of a file under ~/Assets with ?v=&lt;last write time&gt; on the end.
    /// Browsers cache .css and .js hard, so without this an edited script can keep running
    /// from the cache against new markup (and read attributes that no longer exist). The
    /// version changes by itself whenever the file is saved, so nothing needs bumping by hand.
    /// Usage in a page: &lt;script src="&lt;%= AssetUrl.Get("~/Assets/js/NewJob.js") %&gt;"&gt;&lt;/script&gt;
    /// </summary>
    public static class AssetUrl
    {
        // Keyed by path; the stored stamp is rechecked against the file, so a cached entry
        // never outlives an edit. This only saves rebuilding the string on every request.
        private static readonly ConcurrentDictionary<string, Tuple<long, string>> Cache =
            new ConcurrentDictionary<string, Tuple<long, string>>();

        public static string Get(string appRelative)
        {
            string url = VirtualPathUtility.ToAbsolute(appRelative);
            string path = HttpContext.Current.Server.MapPath(appRelative);
            if (!File.Exists(path)) return url;

            long ticks = File.GetLastWriteTimeUtc(path).Ticks;
            Tuple<long, string> hit;
            if (Cache.TryGetValue(appRelative, out hit) && hit.Item1 == ticks) return hit.Item2;

            string versioned = url + "?v=" + ticks.ToString("x");
            Cache[appRelative] = Tuple.Create(ticks, versioned);
            return versioned;
        }
    }
}

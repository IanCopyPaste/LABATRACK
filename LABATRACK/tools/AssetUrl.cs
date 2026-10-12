using System;
using System.Collections.Concurrent;
using System.IO;
using System.Web;

namespace LABATRACK.tools
{
    public static class AssetUrl
    {
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

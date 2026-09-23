using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace LABATRACK.Helpers
{
    /// <summary>One named series of numbers, drawn in one colour.</summary>
    public class ChartSeries
    {
        public string Name { get; private set; }
        public string Color { get; private set; }
        public double[] Values { get; private set; }   // double.NaN = no data at that point (a gap, not a zero)

        public ChartSeries(string name, string color, double[] values)
        {
            Name = name; Color = color; Values = values;
        }
    }

    /// <summary>
    /// Draws the report charts as inline SVG on the server, so they need no chart library,
    /// nothing from a CDN, and still show with scripting off and on paper. Assets/js/Charts.js
    /// only adds the hover and keyboard tooltips on top.
    ///
    /// The look follows one set of rules everywhere: bars at most 24px wide with a 4px rounded
    /// top and a square base, a 2px white gap between stacked segments, 2px lines with ringed
    /// end dots, solid hairline gridlines, text in ink colours (never the series colour), and
    /// a legend whenever there are two or more series. Every chart also comes with a table
    /// (see Table), so no value can only be read by hovering.
    /// </summary>
    public static class SvgChart
    {
        // Series colours, checked with the dataviz palette validator against the white card:
        // blue/orange pass colour-blind separation (worst ΔE 26.4) and 3:1 contrast.
        public const string Blue = "#2f62c9";     // slot 1: the main series (cash, this period)
        public const string Orange = "#eb6834";   // slot 2: the second series (e-wallet)
        public const string Gray = "#b8c1cc";     // context only: last period, the years not selected

        // One-hue ramp for the heatmap, light (little) to dark (a lot).
        private static readonly string[] Ramp = { "#eef3fb", "#cde2fb", "#9ec5f4", "#6da7ec", "#3987e5", "#256abf", "#184f95" };

        // Charts are drawn at the width they are shown at (a full-width card or a half one),
        // so their text comes out at its real size instead of being stretched.
        public const int Wide = 1150, Half = 560;
        private const int PadLeft = 58, PadRight = 14, PadTop = 18, PadBottom = 30;
        private const string GridColor = "#e3e8ef", AxisColor = "#cfd7e2";

        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        // ---------------------------------------------------------------- Columns

        /// <summary>
        /// Vertical columns, stacked when there is more than one series. Each column is one
        /// hover/focus target carrying every series at that point. With links, clicking a
        /// column opens that period (a month opens its days, a day opens its hours).
        /// With one series, highlight picks the column in the series colour and greys the rest.
        /// </summary>
        public static string Columns(string ariaLabel, string[] labels, string[] tipTitles, IList<ChartSeries> series,
                                     Func<double, string> format, int height = 260, int labelEvery = 1,
                                     string[] links = null, int highlight = -1, bool labelMax = true, int width = Half,
                                     Func<double, string> tipFormat = null)
        {
            int n = labels.Length;
            double[] totals = Enumerable.Range(0, n).Select(i => series.Sum(s => Safe(s.Values[i]))).ToArray();
            double top, step;
            NiceScale(totals.DefaultIfEmpty(0).Max(), out top, out step);

            double plotW = width - PadLeft - PadRight, plotH = height - PadTop - PadBottom;
            double band = plotW / n;
            double barW = Math.Min(24, band * 0.62);
            Func<double, double> y = v => PadTop + plotH - (v / top) * plotH;

            var sb = Open(ariaLabel, width, height);
            Grid(sb, top, step, y, format, width);

            int maxIndex = Array.IndexOf(totals, totals.Max());
            for (int i = 0; i < n; i++)
            {
                double x = PadLeft + band * i + (band - barW) / 2;
                double running = 0;
                int lastDrawn = -1;
                for (int s = 0; s < series.Count; s++) if (Safe(series[s].Values[i]) > 0) lastDrawn = s;

                sb.Append("<g class=\"col\">");
                bool first = true;
                for (int s = 0; s < series.Count; s++)
                {
                    double v = Safe(series[s].Values[i]);
                    if (v <= 0) continue;
                    double y0 = y(running), y1 = y(running + v);
                    running += v;
                    if (!first) y0 -= 2;                       // the 2px surface gap between stacked segments
                    first = false;
                    if (y0 - y1 < 0.5) continue;
                    string color = series.Count == 1 && highlight >= 0 && i != highlight ? Gray : series[s].Color;
                    sb.AppendFormat(Inv, "<path d=\"{0}\" fill=\"{1}\"/>", Bar(x, y1, barW, y0 - y1, s == lastDrawn), color);
                }
                sb.Append("</g>");

                if (labelMax && i == maxIndex && totals[i] > 0)
                    sb.AppendFormat(Inv, "<text x=\"{0:0.#}\" y=\"{1:0.#}\" text-anchor=\"middle\" class=\"chart-value\">{2}</text>",
                        x + barW / 2, y(totals[i]) - 6, Enc(format(totals[i])));

                XLabel(sb, i, n, labelEvery, PadLeft + band * i + band / 2, height, labels[i]);
                HitTarget(sb, PadLeft + band * i, PadTop, band, plotH, tipTitles[i], series, i, tipFormat ?? format,
                          links != null ? links[i] : null, -1);
            }

            Baseline(sb, y(0), width);
            return Close(sb);
        }

        // ---------------------------------------------------------------- Lines

        /// <summary>
        /// Lines over the same x positions as Columns. A NaN value leaves a gap (for example the
        /// months of this year that have not happened yet). The pointer snaps to the nearest x,
        /// and one tooltip lists every series there, with a hairline marking the spot.
        /// </summary>
        public static string Lines(string ariaLabel, string[] labels, string[] tipTitles, IList<ChartSeries> series,
                                   Func<double, string> format, int height = 240, int labelEvery = 1, int width = Half,
                                   Func<double, string> tipFormat = null)
        {
            int n = labels.Length;
            double max = series.SelectMany(s => s.Values).Where(v => !double.IsNaN(v)).DefaultIfEmpty(0).Max();
            double top, step;
            NiceScale(max, out top, out step);

            double plotW = width - PadLeft - PadRight, plotH = height - PadTop - PadBottom;
            double band = plotW / n;
            Func<int, double> x = i => PadLeft + band * i + band / 2;
            Func<double, double> y = v => PadTop + plotH - (v / top) * plotH;

            var sb = Open(ariaLabel, width, height);
            Grid(sb, top, step, y, format, width);
            Baseline(sb, y(0), width);
            sb.AppendFormat(Inv, "<line class=\"crosshair\" x1=\"0\" x2=\"0\" y1=\"{0}\" y2=\"{1:0.#}\" stroke=\"{2}\" stroke-width=\"1\"/>",
                PadTop, PadTop + plotH, AxisColor);

            // Draw the context series first so the main one sits on top.
            foreach (var s in series.Reverse())
            {
                var path = new StringBuilder();
                bool pen = false;
                for (int i = 0; i < n; i++)
                {
                    if (double.IsNaN(s.Values[i])) { pen = false; continue; }
                    path.AppendFormat(Inv, "{0}{1:0.#},{2:0.#} ", pen ? "L" : "M", x(i), y(s.Values[i]));
                    pen = true;
                }
                sb.AppendFormat(Inv, "<path d=\"{0}\" fill=\"none\" stroke=\"{1}\" stroke-width=\"2\" stroke-linejoin=\"round\" stroke-linecap=\"round\"/>",
                    path.ToString().Trim(), s.Color);

                int last = Array.FindLastIndex(s.Values, v => !double.IsNaN(v));
                if (last >= 0)
                    sb.AppendFormat(Inv, "<circle cx=\"{0:0.#}\" cy=\"{1:0.#}\" r=\"4\" fill=\"{2}\" stroke=\"#fff\" stroke-width=\"2\"/>",
                        x(last), y(s.Values[last]), s.Color);
            }

            // One direct label: the latest value of the main series, where the reader looks first.
            int end = Array.FindLastIndex(series[0].Values, v => !double.IsNaN(v));
            if (end >= 0)
            {
                bool nearRight = x(end) > width - 90;
                sb.AppendFormat(Inv, "<text x=\"{0:0.#}\" y=\"{1:0.#}\" text-anchor=\"{2}\" class=\"chart-value\">{3}</text>",
                    x(end) + (nearRight ? -8 : 8), y(series[0].Values[end]) - 8, nearRight ? "end" : "start", Enc(format(series[0].Values[end])));
            }

            for (int i = 0; i < n; i++)
            {
                XLabel(sb, i, n, labelEvery, x(i), height, labels[i]);
                HitTarget(sb, PadLeft + band * i, PadTop, band, plotH, tipTitles[i], series, i, tipFormat ?? format, null, x(i));
            }
            return Close(sb);
        }

        // ---------------------------------------------------------------- Horizontal bars (HTML)

        /// <summary>
        /// Labelled horizontal bars, for categories with names (services, add-ons, staff).
        /// One series, so one colour: a darker bar would only repeat what its length says.
        /// </summary>
        public static string HBars(IList<string> labels, IList<double> values, IList<string> valueText, IList<string> tipRows, string tipName)
        {
            double max = values.DefaultIfEmpty(0).Max();
            var sb = new StringBuilder("<div class=\"hbars\">");
            for (int i = 0; i < labels.Count; i++)
            {
                double pct = max > 0 ? values[i] / max * 100 : 0;
                sb.Append("<div class=\"hbar\" tabindex=\"0\"")
                  .Append(TipAttrs(labels[i], new[] { new[] { tipName, tipRows[i], Blue } }))
                  .Append("><span class=\"hbar-label\">").Append(Enc(labels[i])).Append("</span>")
                  .AppendFormat(Inv, "<span class=\"hbar-track\"><span class=\"hbar-fill\" style=\"width:{0:0.#}%\"></span></span>", pct)
                  .Append("<span class=\"hbar-value\">").Append(Enc(valueText[i])).Append("</span></div>");
            }
            return sb.Append("</div>").ToString();
        }

        // ---------------------------------------------------------------- Heatmap (HTML)

        /// <summary>
        /// A grid of counts, darker where there are more. Used for "busiest times":
        /// days of the week down the side, hours across the top.
        /// </summary>
        public static string Heatmap(string[] rowLabels, string[] colLabels, int[,] counts, string unit)
        {
            int max = 0;
            foreach (int c in counts) max = Math.Max(max, c);

            var sb = new StringBuilder();
            sb.AppendFormat(Inv, "<div class=\"heatmap\" style=\"grid-template-columns: 44px repeat({0}, minmax(0, 1fr));\">", colLabels.Length);
            sb.Append("<span></span>");
            foreach (var c in colLabels) sb.Append("<span class=\"heat-col\">").Append(Enc(c)).Append("</span>");
            for (int r = 0; r < rowLabels.Length; r++)
            {
                sb.Append("<span class=\"heat-row\">").Append(Enc(rowLabels[r])).Append("</span>");
                for (int c = 0; c < colLabels.Length; c++)
                {
                    int v = counts[r, c];
                    string fill = v == 0 ? "#f6f8fb" : Ramp[Math.Min(Ramp.Length - 1, (int)Math.Ceiling((double)v / max * (Ramp.Length - 1)))];
                    sb.AppendFormat(Inv, "<span class=\"heat-cell\" tabindex=\"0\" style=\"background:{0}\"", fill)
                      .Append(TipAttrs(rowLabels[r] + ", " + colLabels[c], new[] { new[] { unit, v.ToString("N0", Inv), "" } }))
                      .Append("></span>");
                }
            }
            sb.Append("</div>");

            // The scale says what the shades mean, from none to the busiest hour.
            sb.Append("<div class=\"heat-scale\"><span>Fewer</span>");
            foreach (var step in Ramp) sb.AppendFormat("<i style=\"background:{0}\"></i>", step);
            sb.Append("<span>More</span><span class=\"muted\">&middot; busiest hour: ").Append(max.ToString("N0", Inv))
              .Append(' ').Append(Enc(unit.ToLowerInvariant())).Append("</span></div>");
            return sb.ToString();
        }

        // ---------------------------------------------------------------- Legend and table

        /// <summary>Legend keys mirror the marks: a square for bars, a short stroke for lines.</summary>
        public static string Legend(IList<ChartSeries> series, bool lines = false)
        {
            var sb = new StringBuilder("<div class=\"legend\">");
            foreach (var s in series)
                sb.Append("<span class=\"legend-item\"><i class=\"").Append(lines ? "key-line" : "key-box")
                  .Append("\" style=\"background:").Append(s.Color).Append("\"></i>").Append(Enc(s.Name)).Append("</span>");
            return sb.Append("</div>").ToString();
        }

        /// <summary>
        /// The same numbers as a table, folded away under the chart. This is the version a
        /// screen reader, a printout, or anyone who cannot tell the colours apart can read.
        /// </summary>
        public static string Table(string summary, string[] headers, IEnumerable<string[]> rows)
        {
            var sb = new StringBuilder("<details class=\"chart-table\"><summary>");
            sb.Append(Enc(summary)).Append("</summary><div class=\"table-wrap\"><table class=\"table\"><thead><tr>");
            for (int i = 0; i < headers.Length; i++)
                sb.Append(i == 0 ? "<th>" : "<th class=\"right\">").Append(Enc(headers[i])).Append("</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (var row in rows)
            {
                sb.Append("<tr>");
                for (int i = 0; i < row.Length; i++)
                    sb.Append(i == 0 ? "<td>" : "<td class=\"right num\">").Append(Enc(row[i])).Append("</td>");
                sb.Append("</tr>");
            }
            return sb.Append("</tbody></table></div></details>").ToString();
        }

        // ---------------------------------------------------------------- Formatting

        public static string Peso(double v)
        {
            return "₱" + v.ToString("N0", Inv);
        }

        /// <summary>Short peso amounts for axis ticks and bar tips: ₱850, ₱12.4k, ₱1.2M.</summary>
        public static string PesoShort(double v)
        {
            if (v >= 1000000) return "₱" + (v / 1000000).ToString("0.#", Inv) + "M";
            if (v >= 1000) return "₱" + (v / 1000).ToString(v >= 10000 ? "0" : "0.#", Inv) + "k";
            return "₱" + v.ToString("0", Inv);
        }

        // ---------------------------------------------------------------- Pieces

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static double Safe(double v)
        {
            return double.IsNaN(v) ? 0 : v;
        }

        private static StringBuilder Open(string ariaLabel, int width, int height)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(Inv, "<svg class=\"chart\" viewBox=\"0 0 {0} {1}\" role=\"img\" aria-label=\"{2}\">",
                width, height, HttpUtility.HtmlAttributeEncode(ariaLabel));
            return sb;
        }

        private static string Close(StringBuilder sb)
        {
            return sb.Append("</svg>").ToString();
        }

        private static void Grid(StringBuilder sb, double top, double step, Func<double, double> y, Func<double, string> format, int width)
        {
            for (double v = step; v <= top + step / 2; v += step)
            {
                sb.AppendFormat(Inv, "<line x1=\"{0}\" x2=\"{1}\" y1=\"{2:0.#}\" y2=\"{2:0.#}\" stroke=\"{3}\" stroke-width=\"1\"/>",
                    PadLeft, width - PadRight, y(v), GridColor);
            }
            for (double v = 0; v <= top + step / 2; v += step)
                sb.AppendFormat(Inv, "<text x=\"{0}\" y=\"{1:0.#}\" text-anchor=\"end\" class=\"chart-axis\">{2}</text>",
                    PadLeft - 8, y(v) + 4, Enc(format(v)));
        }

        private static void Baseline(StringBuilder sb, double y0, int width)
        {
            sb.AppendFormat(Inv, "<line x1=\"{0}\" x2=\"{1}\" y1=\"{2:0.#}\" y2=\"{2:0.#}\" stroke=\"{3}\" stroke-width=\"1\"/>",
                PadLeft, width - PadRight, y0, AxisColor);
        }

        private static void XLabel(StringBuilder sb, int i, int n, int every, double x, int height, string label)
        {
            // Always label the first and last, then every Nth, so the ends are never a guess.
            if (i % every != 0 && i != n - 1) return;
            if (i == n - 1 && i % every != 0 && (n - 1) % every <= every / 2.0) return;   // too close to the one before
            sb.AppendFormat(Inv, "<text x=\"{0:0.#}\" y=\"{1}\" text-anchor=\"middle\" class=\"chart-axis\">{2}</text>",
                x, height - 10, Enc(label));
        }

        private static void HitTarget(StringBuilder sb, double x, double y, double w, double h, string title,
                                      IList<ChartSeries> series, int i, Func<double, string> format, string link, double crossX)
        {
            var rows = series.Select(s => new[] { s.Name, double.IsNaN(s.Values[i]) ? "—" : format(s.Values[i]), s.Color }).ToList();
            // A stacked column also reads out its whole height; lines are separate things, so they do not.
            if (crossX < 0 && series.Count > 1)
                rows.Add(new[] { "Total", format(series.Sum(s => Safe(s.Values[i]))), "" });
            string rect = string.Format(Inv, "<rect class=\"hit\" x=\"{0:0.#}\" y=\"{1}\" width=\"{2:0.#}\" height=\"{3:0.#}\"", x, y, w, h);
            string cross = crossX >= 0 ? string.Format(Inv, " data-x=\"{0:0.#}\"", crossX) : "";

            if (link != null)
                sb.Append("<a href=\"").Append(HttpUtility.HtmlAttributeEncode(link)).Append("\"").Append(TipAttrs(title, rows.ToArray())).Append(cross)
                  .Append(">").Append(rect).Append("/></a>");
            else
                sb.Append(rect).Append(" tabindex=\"0\"").Append(TipAttrs(title, rows.ToArray())).Append(cross).Append("/>");
        }

        /// <summary>
        /// The tooltip travels as data attributes (title + rows as JSON). Charts.js reads them and
        /// builds the tooltip with textContent, so a customer name can never inject markup.
        /// </summary>
        private static string TipAttrs(string title, string[][] rows)
        {
            return " data-tip-title=\"" + HttpUtility.HtmlAttributeEncode(title) + "\" data-tip-rows=\""
                 + HttpUtility.HtmlAttributeEncode(Json.Serialize(rows)) + "\"";
        }

        /// <summary>A bar with a 4px rounded data end and a square base; lower stacked segments stay square.</summary>
        private static string Bar(double x, double y, double w, double h, bool roundTop)
        {
            double r = roundTop ? Math.Min(4, Math.Min(h, w / 2)) : 0;
            return string.Format(Inv, "M{0:0.##},{1:0.##} V{2:0.##} Q{0:0.##},{3:0.##} {4:0.##},{3:0.##} H{5:0.##} Q{6:0.##},{3:0.##} {6:0.##},{2:0.##} V{1:0.##} Z",
                x, y + h, y + r, y, x + r, x + w - r, x + w);
        }

        /// <summary>A round top for the axis (0, 1, 2, 2.5, 5 × a power of ten) and about four gridlines.</summary>
        private static void NiceScale(double max, out double top, out double step)
        {
            if (max <= 0) { top = 4; step = 1; return; }
            double raw = max / 4;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double norm = raw / mag;
            double nice = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 2.5 ? 2.5 : norm <= 5 ? 5 : 10;
            step = nice * mag;
            top = Math.Ceiling(max / step) * step;
        }

        private static string Enc(string s)
        {
            return HttpUtility.HtmlEncode(s);
        }
    }
}

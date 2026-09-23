using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using LABATRACK.Helpers;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    // DRAFT: every figure comes from Helpers/SampleSales (made-up history). Phase 5 moves the
    // sums into ReportService on the database; the charts (Helpers/SvgChart) stay as they are.
    //
    // One period is chosen at the top (a year, a month, or a day) and every chart and number
    // below it covers that period only, so they always agree. Comparisons are like for like:
    // this year so far against the same dates last year, this month so far against the same
    // days last month, and a day against the same weekday a week before. A part-period is
    // never compared with a whole one.
    //
    // Two date rules from CLAUDE.md: money is counted on the payment date, which here is the
    // drop-off (everything is paid in full at drop-off); service popularity is counted on the
    // job's created date, which is the same moment. A voided job is paid and refunded the same
    // day, so it adds nothing to sales but is still counted under voids.
    public partial class Reports : BasePage
    {
        protected override string ActiveNav
        {
            get { return "Reports"; }
        }

        protected override string PageStyleSheet
        {
            get { return "Reports.css"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly string[] Weekdays = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        protected string View { get; private set; }           // year, month or day
        protected DateTime Start { get; private set; }
        protected DateTime End { get; private set; }          // exclusive, and never later than "now"
        protected DateTime PrevStart { get; private set; }
        protected DateTime PrevEnd { get; private set; }
        protected string PeriodTitle { get; private set; }
        protected string PeriodNote { get; private set; }
        protected string PrevLabel { get; private set; }
        protected string PrevLink { get; private set; }
        protected string NextLink { get; private set; }

        private List<SalesJob> jobs, prevJobs;

        protected void Page_Load(object sender, EventArgs e)
        {
            ChoosePeriod();
            jobs = InRange(Start, End);
            prevJobs = InRange(PrevStart, PrevEnd);

            if (!IsPostBack)
            {
                ddlYear.Items.Clear();
                for (int y = SampleSales.Now.Year; y >= SampleSales.Opened.Year; y--)
                    ddlYear.Items.Add(y.ToString(Inv));
                ddlYear.SelectedValue = Start.Year.ToString(Inv);
                txtMonth.Text = Start.ToString("yyyy-MM", Inv);
                txtDay.Text = Start.ToString("yyyy-MM-dd", Inv);
                txtDay.Attributes["min"] = SampleSales.Opened.ToString("yyyy-MM-dd", Inv);
                txtDay.Attributes["max"] = SampleSales.Now.ToString("yyyy-MM-dd", Inv);
                txtMonth.Attributes["min"] = SampleSales.Opened.ToString("yyyy-MM", Inv);
                txtMonth.Attributes["max"] = SampleSales.Now.ToString("yyyy-MM", Inv);
            }
        }

        protected void btnShow_Click(object sender, EventArgs e)
        {
            string url;
            // The view comes from the address (the tabs are links); the box that goes with it holds the period.
            switch (View)
            {
                case "year": url = "Reports.aspx?view=year&y=" + HttpUtility.UrlEncode(ddlYear.SelectedValue); break;
                case "day": url = "Reports.aspx?view=day&d=" + HttpUtility.UrlEncode(txtDay.Text); break;
                default: url = "Reports.aspx?view=month&m=" + HttpUtility.UrlEncode(txtMonth.Text); break;
            }
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ================================================================ Period

        private void ChoosePeriod()
        {
            DateTime now = SampleSales.Now, open = SampleSales.Opened;
            View = Request.QueryString["view"];
            if (View != "year" && View != "day") View = "month";

            if (View == "year")
            {
                int y;
                if (!int.TryParse(Request.QueryString["y"], out y)) y = now.Year;
                y = Math.Min(now.Year, Math.Max(open.Year, y));
                Start = Max(new DateTime(y, 1, 1), open);
                End = Min(new DateTime(y + 1, 1, 1), now);
                PrevStart = Start.AddYears(-1);
                PrevEnd = End.AddYears(-1);
                PeriodTitle = y.ToString(Inv);
                PeriodNote = Span(Start, End);
                PrevLabel = Span(PrevStart, PrevEnd) + ", " + (y - 1);
                PrevLink = y > open.Year ? "Reports.aspx?view=year&y=" + (y - 1) : null;
                NextLink = y < now.Year ? "Reports.aspx?view=year&y=" + (y + 1) : null;
            }
            else if (View == "month")
            {
                DateTime m;
                if (!DateTime.TryParseExact(Request.QueryString["m"], "yyyy-MM", Inv, DateTimeStyles.None, out m))
                    m = new DateTime(now.Year, now.Month, 1);
                m = Min(Max(m, new DateTime(open.Year, open.Month, 1)), new DateTime(now.Year, now.Month, 1));
                Start = Max(m, open);
                End = Min(m.AddMonths(1), now);
                PrevStart = Start.AddMonths(-1);
                PrevEnd = End.AddMonths(-1);
                PeriodTitle = m.ToString("MMMM yyyy", Inv);
                PeriodNote = Span(Start, End);
                PrevLabel = Span(PrevStart, PrevEnd);
                PrevLink = m > new DateTime(open.Year, open.Month, 1) ? "Reports.aspx?view=month&m=" + m.AddMonths(-1).ToString("yyyy-MM", Inv) : null;
                NextLink = m < new DateTime(now.Year, now.Month, 1) ? "Reports.aspx?view=month&m=" + m.AddMonths(1).ToString("yyyy-MM", Inv) : null;
            }
            else
            {
                DateTime d;
                if (!DateTime.TryParseExact(Request.QueryString["d"], "yyyy-MM-dd", Inv, DateTimeStyles.None, out d))
                    d = now.Date;
                d = Min(Max(d, open), now.Date);
                Start = d;
                End = Min(d.AddDays(1), now);
                PrevStart = d.AddDays(-7);
                PrevEnd = End.AddDays(-7);
                PeriodTitle = d.ToString("dddd, MMMM d, yyyy", Inv);
                PeriodNote = d == now.Date ? "Today, up to " + now.ToString("h:mm tt", Inv) : "The whole day";
                PrevLabel = "the " + d.ToString("dddd", Inv) + " before, " + PrevStart.ToString("MMM d", Inv);
                PrevLink = d > open ? "Reports.aspx?view=day&d=" + d.AddDays(-1).ToString("yyyy-MM-dd", Inv) : null;
                NextLink = d < now.Date ? "Reports.aspx?view=day&d=" + d.AddDays(1).ToString("yyyy-MM-dd", Inv) : null;
            }

            // Nothing happened before the shop opened, so never claim to compare with it.
            if (PrevEnd <= open)
            {
                PrevLabel = "nothing earlier: the shop opened " + open.ToString("MMM d, yyyy", Inv);
            }
            else if (PrevStart < open)
            {
                PrevStart = open;
                PrevLabel = Span(PrevStart, PrevEnd) + ", " + PrevStart.Year
                          + " (the shop opened that day, so the earlier period is shorter)";
            }
        }

        private static string Span(DateTime start, DateTime end)
        {
            DateTime last = end.TimeOfDay == TimeSpan.Zero ? end.AddDays(-1) : end;
            if (start.Date == last.Date) return start.ToString("MMM d", Inv);
            return start.ToString("MMM d", Inv) + " – " + last.ToString("MMM d", Inv);
        }

        private static List<SalesJob> InRange(DateTime from, DateTime to)
        {
            return SampleSales.Jobs.Where(j => j.DroppedOff >= from && j.DroppedOff < to).ToList();
        }

        private static IEnumerable<SalesJob> Live(IEnumerable<SalesJob> list)
        {
            return list.Where(j => !j.Voided);
        }

        private static DateTime Min(DateTime a, DateTime b) { return a < b ? a : b; }
        private static DateTime Max(DateTime a, DateTime b) { return a > b ? a : b; }

        protected string ViewLink(string view)
        {
            if (view == "year") return "Reports.aspx?view=year&y=" + Start.Year;
            if (view == "day") return "Reports.aspx?view=day&d=" + (View == "day" ? Start : Min(End.AddTicks(-1), SampleSales.Now).Date).ToString("yyyy-MM-dd", Inv);
            return "Reports.aspx?view=month&m=" + (View == "year" ? Min(End.AddTicks(-1), SampleSales.Now) : Start).ToString("yyyy-MM", Inv);
        }

        // ================================================================ KPI tiles

        protected string KpiTiles()
        {
            var cur = Live(jobs).ToList();
            var prev = Live(prevJobs).ToList();

            decimal sales = cur.Sum(j => j.Total), prevSales = prev.Sum(j => j.Total);
            int orders = cur.Count, prevOrders = prev.Count;
            decimal avg = orders > 0 ? sales / orders : 0, prevAvg = prevOrders > 0 ? prevSales / prevOrders : 0;
            decimal kg = cur.Sum(j => j.WeightKg), prevKg = prev.Sum(j => j.WeightKg);
            double tat = AvgTurnaround(cur), prevTat = AvgTurnaround(prev);
            double ret = orders > 0 ? cur.Count(j => j.IsReturning) * 100.0 / orders : 0;
            double prevRet = prevOrders > 0 ? prev.Count(j => j.IsReturning) * 100.0 / prevOrders : 0;

            return Tile("Sales", "wallet", Peso2(sales), Delta((double)sales, (double)prevSales, true, Peso0((double)prevSales)), true)
                 + Tile("Job orders", "clipboard-list", orders.ToString("N0", Inv), Delta(orders, prevOrders, true, prevOrders.ToString("N0", Inv)), false)
                 + Tile("Average per order", "receipt", Peso2(avg), Delta((double)avg, (double)prevAvg, true, Peso2(prevAvg)), false)
                 + Tile("Laundry washed", "weight", kg.ToString("N0", Inv) + " kg", Delta((double)kg, (double)prevKg, true, prevKg.ToString("N0", Inv) + " kg"), false)
                 + Tile("Average turnaround", "timer", double.IsNaN(tat) ? "—" : tat.ToString("0.0", Inv) + " h",
                        double.IsNaN(tat) || double.IsNaN(prevTat) ? Note("Not ready yet") : DeltaHours(tat, prevTat), false)
                 + Tile("Repeat business", "users", ret.ToString("0", Inv) + "%",
                        prevOrders == 0 ? Note("No earlier period") : DeltaPoints(ret, prevRet), false);
        }

        private static double AvgTurnaround(IEnumerable<SalesJob> list)
        {
            var hours = list.Where(j => j.ReadyAt.HasValue).Select(j => (j.ReadyAt.Value - j.DroppedOff).TotalHours).ToList();
            return hours.Count == 0 ? double.NaN : hours.Average();
        }

        private string Tile(string label, string icon, string value, string foot, bool hero)
        {
            return "<div class=\"card kpi" + (hero ? " kpi-hero" : "") + "\"><div class=\"kpi-top\">" + HttpUtility.HtmlEncode(label)
                 + " <span class=\"kpi-icon\">" + Icons.Get(icon, "ico-sm") + "</span></div><div class=\"kpi-value\">"
                 + HttpUtility.HtmlEncode(value) + "</div><div class=\"kpi-foot\">" + foot + "</div></div>";
        }

        // A change is shown with an arrow and a word, never colour alone.
        private string Delta(double cur, double prev, bool upIsGood, string prevText)
        {
            if (prev <= 0) return Note("No earlier period to compare");
            double pct = (cur - prev) / prev * 100;
            return Change(pct >= 0, upIsGood, Math.Abs(pct).ToString("0.0", Inv) + "%") + " <span class=\"muted\">vs " + HttpUtility.HtmlEncode(prevText) + "</span>";
        }

        private string DeltaHours(double cur, double prev)
        {
            double diff = cur - prev;
            return Change(diff >= 0, false, Math.Abs(diff).ToString("0.0", Inv) + " h " + (diff > 0 ? "slower" : "faster"))
                 + " <span class=\"muted\">vs " + prev.ToString("0.0", Inv) + " h</span>";
        }

        private string DeltaPoints(double cur, double prev)
        {
            double diff = cur - prev;
            return Change(diff >= 0, true, Math.Abs(diff).ToString("0.0", Inv) + " pts") + " <span class=\"muted\">vs " + prev.ToString("0", Inv) + "%</span>";
        }

        private static string Change(bool up, bool upIsGood, string text)
        {
            bool good = up == upIsGood;
            return "<span class=\"" + (good ? "up" : "down") + "\">" + Icons.Get(up ? "trending-up" : "trending-down", "ico-sm")
                 + " " + HttpUtility.HtmlEncode(text) + "</span>";
        }

        private static string Note(string text)
        {
            return "<span class=\"muted\">" + HttpUtility.HtmlEncode(text) + "</span>";
        }

        // ================================================================ Buckets

        // The main charts split the period into buckets: months of a year, days of a month,
        // or hours of a day (7 AM to 8 PM, the shop's hours).
        private class Bucket
        {
            public string Label, Tip, Link;
            public DateTime From, To;
        }

        private List<Bucket> Buckets()
        {
            var list = new List<Bucket>();
            if (View == "year")
            {
                for (int m = 1; m <= 12; m++)
                {
                    var from = new DateTime(Start.Year, m, 1);
                    bool hasData = from.AddMonths(1) > SampleSales.Opened && from <= SampleSales.Now;
                    bool partial = from.Year == SampleSales.Now.Year && m == SampleSales.Now.Month;
                    list.Add(new Bucket
                    {
                        Label = from.ToString("MMM", Inv), From = from, To = from.AddMonths(1),
                        Tip = from.ToString("MMMM yyyy", Inv) + (partial ? " (to date)" : "") + (hasData ? " · click to open" : ""),
                        Link = hasData ? "Reports.aspx?view=month&m=" + from.ToString("yyyy-MM", Inv) : null,
                    });
                }
            }
            else if (View == "month")
            {
                int days = DateTime.DaysInMonth(Start.Year, Start.Month);
                var first = new DateTime(Start.Year, Start.Month, 1);
                for (int d = 0; d < days; d++)
                {
                    var from = first.AddDays(d);
                    bool hasData = from >= SampleSales.Opened && from <= SampleSales.Now;
                    list.Add(new Bucket
                    {
                        Label = (d + 1).ToString(Inv), From = from, To = from.AddDays(1),
                        Tip = from.ToString("ddd, MMM d", Inv) + (hasData ? " · click to open" : ""),
                        Link = hasData ? "Reports.aspx?view=day&d=" + from.ToString("yyyy-MM-dd", Inv) : null,
                    });
                }
            }
            else
            {
                for (int h = 7; h <= 20; h++)
                {
                    var from = Start.AddHours(h);
                    list.Add(new Bucket
                    {
                        Label = from.ToString("h tt", Inv).Replace(" ", "").ToLowerInvariant(), From = from, To = from.AddHours(1),
                        Tip = from.ToString("h:mm tt", Inv) + " – " + from.AddHours(1).ToString("h:mm tt", Inv),
                    });
                }
            }
            return list;
        }

        private int LabelEvery { get { return View == "month" ? 2 : 1; } }

        private static double[] SumBy(List<Bucket> buckets, IEnumerable<SalesJob> list, Func<SalesJob, bool> where)
        {
            var sel = list.Where(where).ToList();
            return buckets.Select(b => (double)sel.Where(j => j.DroppedOff >= b.From && j.DroppedOff < b.To).Sum(j => j.Total)).ToArray();
        }

        // ================================================================ Sales chart

        protected string SalesChart()
        {
            var b = Buckets();
            var live = Live(jobs).ToList();
            var cash = new ChartSeries("Cash", SvgChart.Blue, SumBy(b, live, j => j.Method == "Cash"));
            var ewallet = new ChartSeries("E-wallet", SvgChart.Orange, SumBy(b, live, j => j.Method == "E-wallet"));
            var series = new List<ChartSeries> { cash, ewallet };

            string title = View == "year" ? "Sales by month" : View == "month" ? "Sales by day" : "Sales by hour";
            string chart = SvgChart.Columns(title + ", cash and e-wallet", b.Select(x => x.Label).ToArray(), b.Select(x => x.Tip).ToArray(),
                series, SvgChart.PesoShort, 300, LabelEvery, View == "day" ? null : b.Select(x => x.Link).ToArray(), width: SvgChart.Wide, tipFormat: SvgChart.Peso);

            double c = cash.Values.Sum(), w = ewallet.Values.Sum(), t = c + w;
            string split = t > 0
                ? "<div class=\"split-line\"><span><i class=\"key-box\" style=\"background:" + SvgChart.Blue + "\"></i>Cash <b>" + SvgChart.Peso(c) + "</b> <span class=\"muted\">" + Pct(c, t) + "</span></span>"
                  + "<span><i class=\"key-box\" style=\"background:" + SvgChart.Orange + "\"></i>E-wallet <b>" + SvgChart.Peso(w) + "</b> <span class=\"muted\">" + Pct(w, t) + "</span></span></div>"
                : "";

            var rows = b.Select((x, i) => new[] { x.Tip.Replace(" · click to open", ""), SvgChart.Peso(cash.Values[i]), SvgChart.Peso(ewallet.Values[i]), SvgChart.Peso(cash.Values[i] + ewallet.Values[i]) });
            return Card(title,
                (View == "day" ? "Payments taken each hour" : "Click a " + (View == "year" ? "month" : "day") + " to open it") + ". Counted on the payment date; voided jobs net to zero.",
                SvgChart.Legend(series) + chart + split
                + SvgChart.Table("Show as table", new[] { View == "year" ? "Month" : View == "month" ? "Day" : "Hour", "Cash", "E-wallet", "Total" }, rows),
                "chart-card wide");
        }

        // ================================================================ Compared with before

        protected string CompareChart()
        {
            if (View == "year") return CompareYear();
            if (View == "month") return CompareMonth();
            return CompareDay();
        }

        private string CompareYear()
        {
            int y = Start.Year;
            string[] labels = Enumerable.Range(1, 12).Select(m => new DateTime(y, m, 1).ToString("MMM", Inv)).ToArray();
            Func<int, int, double> month = (yy, m) =>
            {
                var from = new DateTime(yy, m, 1);
                if (from.AddMonths(1) <= SampleSales.Opened || from > SampleSales.Now) return double.NaN;
                return (double)Live(SampleSales.Jobs).Where(j => j.DroppedOff >= from && j.DroppedOff < from.AddMonths(1)).Sum(j => j.Total);
            };
            var series = new List<ChartSeries>
            {
                new ChartSeries(y.ToString(Inv), SvgChart.Blue, Enumerable.Range(1, 12).Select(m => month(y, m)).ToArray()),
                new ChartSeries((y - 1).ToString(Inv), SvgChart.Gray, Enumerable.Range(1, 12).Select(m => month(y - 1, m)).ToArray()),
            };

            // A month still in progress would plot as a sudden drop next to last year's whole month,
            // so it stays off the line and is compared day for day in the note instead.
            string sub = "Monthly sales, " + y + " in blue and " + (y - 1) + " in grey.";
            DateTime now = SampleSales.Now;
            if (y == now.Year)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1);
                series[0].Values[now.Month - 1] = double.NaN;
                double soFar = (double)Live(SampleSales.Jobs).Where(j => j.DroppedOff >= monthStart && j.DroppedOff < now).Sum(j => j.Total);
                double sameDays = (double)Live(SampleSales.Jobs).Where(j => j.DroppedOff >= monthStart.AddYears(-1) && j.DroppedOff < now.AddYears(-1)).Sum(j => j.Total);
                sub += " " + monthStart.ToString("MMMM", Inv) + " is not over, so it is left off: " + SvgChart.Peso(soFar)
                     + " so far against " + SvgChart.Peso(sameDays) + " on the same days last year.";
            }

            string[] tips = labels.Select(l => l + " sales").ToArray();
            var rows = labels.Select((l, i) => new[] { l, Val(series[0].Values[i]), Val(series[1].Values[i]) });
            return Card("This year against last year", sub,
                SvgChart.Legend(series, true) + SvgChart.Lines("Monthly sales, " + y + " and " + (y - 1), labels, tips, series, SvgChart.PesoShort, tipFormat: SvgChart.Peso)
                + SvgChart.Table("Show as table", new[] { "Month", y.ToString(Inv), (y - 1).ToString(Inv) }, rows), "chart-card");
        }

        private string CompareMonth()
        {
            DateTime m = new DateTime(Start.Year, Start.Month, 1), pm = m.AddMonths(-1);
            int days = Math.Max(DateTime.DaysInMonth(m.Year, m.Month), DateTime.DaysInMonth(pm.Year, pm.Month));
            Func<DateTime, double[]> running = first =>
            {
                var list = Live(SampleSales.Jobs).Where(j => j.DroppedOff >= first && j.DroppedOff < first.AddMonths(1)).ToList();
                var result = new double[days];
                double sum = 0;
                int monthDays = DateTime.DaysInMonth(first.Year, first.Month);
                for (int d = 0; d < days; d++)
                {
                    DateTime day = first.AddDays(d);
                    if (d >= monthDays || day > SampleSales.Now || day.AddDays(1) <= SampleSales.Opened) { result[d] = double.NaN; continue; }
                    sum += (double)list.Where(j => j.DroppedOff.Date == day).Sum(j => j.Total);
                    result[d] = sum;
                }
                return result;
            };
            var series = new List<ChartSeries>
            {
                new ChartSeries(m.ToString("MMMM", Inv), SvgChart.Blue, running(m)),
                new ChartSeries(pm.ToString("MMMM", Inv), SvgChart.Gray, running(pm)),
            };
            string[] labels = Enumerable.Range(1, days).Select(d => d.ToString(Inv)).ToArray();
            string[] tips = labels.Select(d => "Running total to day " + d).ToArray();
            var rows = labels.Select((l, i) => new[] { "Day " + l, Val(series[0].Values[i]), Val(series[1].Values[i]) });
            return Card("Running total against last month", "Sales added up day by day, so a slow start or a strong finish shows at a glance.",
                SvgChart.Legend(series, true) + SvgChart.Lines("Running sales total, this month and last", labels, tips, series, SvgChart.PesoShort, 240, 2, tipFormat: SvgChart.Peso)
                + SvgChart.Table("Show as table", new[] { "Day", series[0].Name, series[1].Name }, rows), "chart-card");
        }

        private string CompareDay()
        {
            var b = Buckets();
            var today = SumBy(b, Live(jobs), j => true);
            // The same weekday over the four weeks before, averaged hour by hour.
            var weeks = Enumerable.Range(1, 4).Select(k => Start.AddDays(-7 * k)).Where(d => d >= SampleSales.Opened).ToList();
            double[] avg = new double[b.Count];
            if (weeks.Count > 0)
                for (int i = 0; i < b.Count; i++)
                    avg[i] = weeks.Average(w => (double)Live(SampleSales.Jobs)
                        .Where(j => j.DroppedOff >= w.Add(b[i].From - Start) && j.DroppedOff < w.Add(b[i].To - Start)).Sum(j => j.Total));
            // Hours that have not happened yet are a gap, not a zero.
            for (int i = 0; i < b.Count; i++) if (b[i].From >= SampleSales.Now) today[i] = double.NaN;

            string weekday = Start.ToString("dddd", Inv);
            var series = new List<ChartSeries>
            {
                new ChartSeries(Start.ToString("MMM d", Inv), SvgChart.Blue, today),
                new ChartSeries("Average " + weekday, SvgChart.Gray, avg),
            };
            var rows = b.Select((x, i) => new[] { x.Tip, Val(today[i]), SvgChart.Peso(avg[i]) });
            return Card("Against a usual " + weekday, "Sales each hour, next to the average of the " + weeks.Count + " " + weekday + "s before.",
                SvgChart.Legend(series, true) + SvgChart.Lines("Hourly sales against the usual " + weekday, b.Select(x => x.Label).ToArray(),
                    b.Select(x => x.Tip).ToArray(), series, SvgChart.PesoShort, 260, 1, SvgChart.Wide, SvgChart.Peso)
                + SvgChart.Table("Show as table", new[] { "Hour", series[0].Name, series[1].Name }, rows), "chart-card");
        }

        // ================================================================ Every year / weekday

        protected string YearsChart()
        {
            if (View != "year") return "";
            int first = SampleSales.Opened.Year, last = SampleSales.Now.Year;
            var years = Enumerable.Range(first, last - first + 1).ToList();
            double[] totals = years.Select(y => (double)Live(SampleSales.Jobs).Where(j => j.DroppedOff.Year == y).Sum(j => j.Total)).ToArray();
            string[] tips = years.Select(y => y + (y == first ? " (opened " + SampleSales.Opened.ToString("MMM d", Inv) + ")" : y == last ? " (to " + SampleSales.Now.ToString("MMM d", Inv) + ")" : "") + " · click to open").ToArray();
            var series = new List<ChartSeries> { new ChartSeries("Sales", SvgChart.Blue, totals) };
            var rows = years.Select((y, i) => new[] { tips[i].Replace(" · click to open", ""), SvgChart.Peso(totals[i]), JobsIn(y).ToString("N0", Inv) });
            return Card("Every year", "Total sales per year since the shop opened. The first and the current year are part-years.",
                SvgChart.Columns("Sales per year", years.Select(y => y.ToString(Inv)).ToArray(), tips, series, SvgChart.PesoShort, 240, 1,
                    years.Select(y => "Reports.aspx?view=year&y=" + y).ToArray(), years.IndexOf(Start.Year), tipFormat: SvgChart.Peso)
                + SvgChart.Table("Show as table", new[] { "Year", "Sales", "Job orders" }, rows), "chart-card");
        }

        private static int JobsIn(int year)
        {
            return Live(SampleSales.Jobs).Count(j => j.DroppedOff.Year == year);
        }

        protected string WeekdayChart()
        {
            if (View == "day") return "";
            var live = Live(jobs).ToList();
            // Average per weekday, not total: a month can hold five Saturdays and four Mondays.
            double[] avg = new double[7];
            for (int w = 0; w < 7; w++)
            {
                var dow = (DayOfWeek)((w + 1) % 7);
                int dayCount = 0;
                for (var d = Start.Date; d < End; d = d.AddDays(1)) if (d.DayOfWeek == dow) dayCount++;
                avg[w] = dayCount == 0 ? 0 : (double)live.Where(j => j.DroppedOff.DayOfWeek == dow).Sum(j => j.Total) / dayCount;
            }
            int best = Array.IndexOf(avg, avg.Max());
            var series = new List<ChartSeries> { new ChartSeries("Average sales", SvgChart.Blue, avg) };
            var rows = Weekdays.Select((d, i) => new[] { d, SvgChart.Peso(avg[i]) });
            return Card("Busiest days of the week", "Average sales on each weekday. " + FullDay(best) + " is the strongest day in this period.",
                SvgChart.Columns("Average sales by weekday", Weekdays, Weekdays.Select(d => FullDay(d)).ToArray(), series, SvgChart.PesoShort, 240, 1, null, best,
                    width: View == "year" ? SvgChart.Wide : SvgChart.Half, tipFormat: SvgChart.Peso)
                + SvgChart.Table("Show as table", new[] { "Weekday", "Average sales" }, rows), "chart-card");
        }

        private static string FullDay(int monFirst)
        {
            return new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }[monFirst];
        }

        private static string FullDay(string shortDay)
        {
            return FullDay(Array.IndexOf(Weekdays, shortDay));
        }

        // ================================================================ Busiest times

        protected string Heatmap()
        {
            if (View == "day") return "";
            var counts = new int[7, 14];
            foreach (var j in jobs)
            {
                int w = ((int)j.DroppedOff.DayOfWeek + 6) % 7;   // Monday first
                int h = j.DroppedOff.Hour - 7;
                if (h >= 0 && h < 14) counts[w, h]++;
            }
            string[] hours = Enumerable.Range(7, 14).Select(h => new DateTime(2000, 1, 1, h, 0, 0).ToString("h tt", Inv).Replace(" ", "").ToLowerInvariant()).ToArray();
            var rows = Weekdays.Select((d, w) => new[] { d }.Concat(Enumerable.Range(0, 14).Select(h => counts[w, h].ToString(Inv))).ToArray());
            return Card("Busiest drop-off times", "Job orders taken in each hour of each weekday. Use it to plan staff shifts and machine loads.",
                SvgChart.Heatmap(Weekdays, hours, counts, "Job orders")
                + SvgChart.Table("Show as table", new[] { "Day" }.Concat(hours).ToArray(), rows), "chart-card wide");
        }

        // ================================================================ Services and add-ons

        protected string ServicesCard()
        {
            var live = Live(jobs).ToList();
            int total = live.Count;
            var rows = SampleSales.Services.Select(s => new
            {
                Name = s,
                Jobs = live.Count(j => j.Service == s),
                Sales = live.Where(j => j.Service == s).Sum(j => j.Total),
                Kg = live.Where(j => j.Service == s).Sum(j => j.WeightKg),
            }).OrderByDescending(r => r.Jobs).ToList();

            return Card("Most availed services", "Job orders by service, counted on the day the job was created.",
                SvgChart.HBars(rows.Select(r => r.Name).ToList(), rows.Select(r => (double)r.Jobs).ToList(),
                    rows.Select(r => r.Jobs.ToString("N0", Inv) + " · " + Pct(r.Jobs, total)).ToList(),
                    rows.Select(r => r.Jobs.ToString("N0", Inv) + " orders · " + Peso0((double)r.Sales)).ToList(), "Orders")
                + SvgChart.Table("Show as table", new[] { "Service", "Orders", "Share", "Laundry (kg)", "Sales" },
                    rows.Select(r => new[] { r.Name, r.Jobs.ToString("N0", Inv), Pct(r.Jobs, total), r.Kg.ToString("N0", Inv), Peso0((double)r.Sales) })),
                "chart-card");
        }

        protected string AddOnsCard()
        {
            var live = Live(jobs).ToList();
            int total = live.Count;
            var rows = SampleSales.AddOnNames.Select(a => new
            {
                Name = a,
                Jobs = live.Count(j => j.AddOns.Any(x => x.Name == a)),
                Qty = live.SelectMany(j => j.AddOns).Where(x => x.Name == a).Sum(x => x.Quantity),
                Sales = live.SelectMany(j => j.AddOns).Where(x => x.Name == a).Sum(x => x.Amount),
                Product = a == "Fabric conditioner" || a == "Detergent sachet",
            }).OrderByDescending(r => r.Jobs).ToList();
            decimal addOnSales = rows.Sum(r => r.Sales), allSales = live.Sum(j => j.Total);

            return Card("Add-ons", "How often each add-on is taken, as a share of all orders. Add-ons made "
                    + Pct((double)addOnSales, (double)allSales) + " of sales (" + Peso0((double)addOnSales) + ").",
                SvgChart.HBars(rows.Select(r => r.Name + (r.Product ? " (product)" : "")).ToList(),
                    rows.Select(r => total == 0 ? 0 : r.Jobs * 100.0 / total).ToList(),
                    rows.Select(r => Pct(r.Jobs, total) + " · " + Peso0((double)r.Sales)).ToList(),
                    rows.Select(r => r.Jobs.ToString("N0", Inv) + " orders" + (r.Product ? ", " + r.Qty.ToString("N0", Inv) + " pieces" : "") + " · " + Peso0((double)r.Sales)).ToList(), "Taken")
                + SvgChart.Table("Show as table", new[] { "Add-on", "Orders with it", "Share of orders", "Pieces", "Sales" },
                    rows.Select(r => new[] { r.Name, r.Jobs.ToString("N0", Inv), Pct(r.Jobs, total), r.Product ? r.Qty.ToString("N0", Inv) : "—", Peso0((double)r.Sales) })),
                "chart-card");
        }

        // ================================================================ Load sizes and turnaround

        protected string LoadSizeCard()
        {
            var live = Live(jobs).ToList();
            string[] labels = { "Under 2", "2–3", "3–4", "4–5", "5–6", "6–8", "8–10", "10+" };
            double[] edges = { 0, 2, 3, 4, 5, 6, 8, 10, double.MaxValue };
            double[] counts = new double[labels.Length];
            foreach (var j in live)
            {
                double kg = (double)j.WeightKg;
                for (int i = 0; i < labels.Length; i++) if (kg >= edges[i] && kg < edges[i + 1]) { counts[i]++; break; }
            }
            int atMinimum = live.Count(j => j.MinimumApplied);
            double avgKg = live.Count == 0 ? 0 : (double)live.Average(j => j.WeightKg);
            var series = new List<ChartSeries> { new ChartSeries("Orders", SvgChart.Blue, counts) };
            return Card("Load sizes", "Orders by weight in kilos. Average load " + avgKg.ToString("0.0", Inv) + " kg; "
                    + Pct(atMinimum, live.Count) + " were billed at the ₱120 minimum.",
                SvgChart.Columns("Orders by load weight", labels, labels.Select(l => l + " kg").ToArray(), series, v => v.ToString("N0", Inv), 230)
                + SvgChart.Table("Show as table", new[] { "Weight (kg)", "Orders", "Share" },
                    labels.Select((l, i) => new[] { l, counts[i].ToString("N0", Inv), Pct(counts[i], live.Count) })),
                "chart-card");
        }

        protected string TurnaroundCard()
        {
            var hours = Live(jobs).Where(j => j.ReadyAt.HasValue).Select(j => (j.ReadyAt.Value - j.DroppedOff).TotalHours).ToList();
            string[] labels = { "Under 12", "12–18", "18–24", "24–30", "30–36", "36–48", "48+" };
            double[] edges = { 0, 12, 18, 24, 30, 36, 48, double.MaxValue };
            double[] counts = new double[labels.Length];
            foreach (double h in hours)
                for (int i = 0; i < labels.Length; i++) if (h >= edges[i] && h < edges[i + 1]) { counts[i]++; break; }
            int onTime = hours.Count(h => h <= 24);
            var series = new List<ChartSeries> { new ChartSeries("Orders", SvgChart.Blue, counts) };
            string sub = hours.Count == 0
                ? "No load from this period has reached Ready for pick-up yet."
                : "Hours from drop-off to Ready for pick-up. " + Pct(onTime, hours.Count) + " were ready within the 24-hour turnaround.";
            return Card("Turnaround", sub,
                SvgChart.Columns("Orders by turnaround time", labels, labels.Select(l => l + " hours").ToArray(), series, v => v.ToString("N0", Inv), 230)
                + SvgChart.Table("Show as table", new[] { "Hours", "Orders", "Share" },
                    labels.Select((l, i) => new[] { l, counts[i].ToString("N0", Inv), Pct(counts[i], hours.Count) })),
                "chart-card");
        }

        // ================================================================ Customers

        protected string CustomersCard()
        {
            var live = Live(jobs).ToList();
            int distinct = live.Select(j => j.CustomerId).Distinct().Count();
            int newOnes = live.Count(j => !j.IsReturning);

            string chart = "";
            if (View != "day")
            {
                var b = Buckets();
                Func<Func<SalesJob, bool>, double[]> count = f => b.Select(x => (double)live.Count(j => f(j) && j.DroppedOff >= x.From && j.DroppedOff < x.To)).ToArray();
                var series = new List<ChartSeries>
                {
                    new ChartSeries("Returning", SvgChart.Blue, count(j => j.IsReturning)),
                    new ChartSeries("New", SvgChart.Orange, count(j => !j.IsReturning)),
                };
                chart = SvgChart.Legend(series) + SvgChart.Columns("Orders from new and returning customers", b.Select(x => x.Label).ToArray(),
                            b.Select(x => x.Tip.Replace(" · click to open", "")).ToArray(), series, v => v.ToString("N0", Inv), 230, LabelEvery, null, -1, false)
                      + SvgChart.Table("Show as table", new[] { View == "year" ? "Month" : "Day", "Returning", "New" },
                            b.Select((x, i) => new[] { x.Tip.Replace(" · click to open", ""), series[0].Values[i].ToString("N0", Inv), series[1].Values[i].ToString("N0", Inv) }));
            }

            var stats = "<div class=\"mini-stats\">"
                + Mini("Customers served", distinct.ToString("N0", Inv))
                + Mini("First-time customers", newOnes.ToString("N0", Inv))
                + Mini("Orders from regulars", Pct(live.Count - newOnes, live.Count))
                + "</div>";

            return Card("New and returning customers", "Each order is from a first-time customer or from someone who has come before. Repeat business is what keeps a laundry shop steady.",
                stats + chart, "chart-card");
        }

        protected string TopCustomersCard()
        {
            var live = Live(jobs).ToList();
            var top = live.GroupBy(j => j.CustomerId)
                .Select(g => new { Customer = SampleSales.Customers[g.Key - 1], Orders = g.Count(), Sales = g.Sum(j => j.Total), Kg = g.Sum(j => j.WeightKg) })
                .OrderByDescending(x => x.Sales).ThenByDescending(x => x.Orders).Take(8).ToList();

            var sb = new System.Text.StringBuilder("<div class=\"table-wrap\"><table class=\"table\"><thead><tr><th>Customer</th><th class=\"right\">Orders</th><th class=\"right\">Laundry</th><th class=\"right\">Sales</th></tr></thead><tbody>");
            foreach (var t in top)
                sb.Append("<tr><td><div class=\"cell-main\">").Append(HttpUtility.HtmlEncode(t.Customer.Name))
                  .Append("</div><div class=\"cell-sub\">Customer since ").Append(t.Customer.FirstVisit.ToString("MMM yyyy", Inv)).Append("</div></td>")
                  .Append("<td class=\"right num\">").Append(t.Orders.ToString("N0", Inv)).Append("</td>")
                  .Append("<td class=\"right num\">").Append(t.Kg.ToString("N0", Inv)).Append(" kg</td>")
                  .Append("<td class=\"right num strong\">").Append(Peso0((double)t.Sales)).Append("</td></tr>");
            if (top.Count == 0) sb.Append("<tr><td colspan=\"4\" class=\"muted\">No orders in this period.</td></tr>");
            sb.Append("</tbody></table></div>");
            return Card("Top customers", "By sales in this period. Worth a thank-you, or a word if they stop coming.", sb.ToString(), "chart-card flush");
        }

        // ================================================================ Staff and voids

        protected string StaffCard()
        {
            var rows = SampleSales.Staff.Select(s => new
            {
                Name = s,
                Orders = jobs.Count(j => j.Staff == s && !j.Voided),
                Voids = jobs.Count(j => j.Staff == s && j.Voided),
                Sales = jobs.Where(j => j.Staff == s && !j.Voided).Sum(j => j.Total),
            }).Where(r => r.Orders + r.Voids > 0).OrderByDescending(r => r.Orders).ToList();
            int total = rows.Sum(r => r.Orders);

            return Card("Orders taken by staff", "Who took each job order at the counter. For planning shifts, not for pay.",
                SvgChart.HBars(rows.Select(r => r.Name).ToList(), rows.Select(r => (double)r.Orders).ToList(),
                    rows.Select(r => r.Orders.ToString("N0", Inv) + " · " + Pct(r.Orders, total)).ToList(),
                    rows.Select(r => r.Orders.ToString("N0", Inv) + " orders · " + Peso0((double)r.Sales) + " · " + r.Voids + " voided").ToList(), "Taken")
                + SvgChart.Table("Show as table", new[] { "Staff", "Orders", "Share", "Sales", "Voided" },
                    rows.Select(r => new[] { r.Name, r.Orders.ToString("N0", Inv), Pct(r.Orders, total), Peso0((double)r.Sales), r.Voids.ToString("N0", Inv) })),
                "chart-card");
        }

        protected string VoidsCard()
        {
            var voided = jobs.Where(j => j.Voided).ToList();
            decimal refunded = voided.Sum(j => j.Total);
            var reasons = voided.GroupBy(j => j.VoidReason).Select(g => new { Reason = g.Key, Count = g.Count(), Amount = g.Sum(j => j.Total) })
                .OrderByDescending(r => r.Count).ToList();

            var sb = new System.Text.StringBuilder("<div class=\"mini-stats\">");
            sb.Append(Mini("Voided orders", voided.Count.ToString("N0", Inv)))
              .Append(Mini("Refunded", Peso0((double)refunded)))
              .Append(Mini("Void rate", Pct(voided.Count, jobs.Count)))
              .Append("</div>");
            if (reasons.Count > 0)
            {
                sb.Append("<div class=\"table-wrap\"><table class=\"table\"><thead><tr><th>Reason given</th><th class=\"right\">Orders</th><th class=\"right\">Refunded</th></tr></thead><tbody>");
                foreach (var r in reasons)
                    sb.Append("<tr><td>").Append(HttpUtility.HtmlEncode(r.Reason)).Append("</td><td class=\"right num\">").Append(r.Count)
                      .Append("</td><td class=\"right num\">").Append(Peso0((double)r.Amount)).Append("</td></tr>");
                sb.Append("</tbody></table></div>");
            }
            else
            {
                sb.Append("<p class=\"muted small\" style=\"padding:0 20px 18px;\">No orders were voided in this period.</p>");
            }
            return Card("Voids and refunds", "Every void is refunded in full, so none of this is in the sales figures. Look at the reasons, not just the count.",
                sb.ToString(), "chart-card flush");
        }

        // ================================================================ Pieces

        private static string Card(string title, string sub, string body, string css)
        {
            return "<section class=\"card " + css + "\"><div class=\"card-h\"><div><h2>" + HttpUtility.HtmlEncode(title)
                 + "</h2><div class=\"sub\">" + HttpUtility.HtmlEncode(sub) + "</div></div></div><div class=\"card-b\">" + body + "</div></section>";
        }

        private static string Mini(string label, string value)
        {
            return "<div class=\"mini-stat\"><div class=\"muted small\">" + HttpUtility.HtmlEncode(label) + "</div><div class=\"mini-value\">"
                 + HttpUtility.HtmlEncode(value) + "</div></div>";
        }

        private static string Pct(double part, double whole)
        {
            return whole <= 0 ? "0%" : (part * 100 / whole).ToString(part * 100 / whole < 10 ? "0.0" : "0", Inv) + "%";
        }

        private static string Val(double v)
        {
            return double.IsNaN(v) ? "—" : SvgChart.Peso(v);
        }

        private static string Peso0(double v) { return SvgChart.Peso(v); }
        private static string Peso2(decimal v) { return "₱" + v.ToString("N2", Inv); }
    }
}

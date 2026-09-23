using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using LABATRACK.Helpers;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    // DRAFT: one day of payments from Helpers/SampleSales, the same made-up history the Reports
    // page uses, so Sep 22 shows the same ₱850 as the dashboards. Phase 5 reads Payments where
    // PaidAt falls inside the chosen day (PaidAt >= @day AND PaidAt < @nextDay) instead.
    //
    // The page is laid out as one A4 sheet. On screen it is shown as a sheet of paper with a
    // toolbar above it; printing drops the toolbar and prints the sheet as it is, with its own
    // page margins and page numbers, rather than a copy of the screen.
    public partial class DailySalesReport : BasePage
    {
        protected override string PageStyleSheet
        {
            get { return "DailySalesReport.css"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string ShopName = "LabaTrack Laundry Shop";   // DRAFT: Phase 2 reads it from Settings
        private const string PreparedBy = "owner";                  // DRAFT: the signed-in user

        protected DateTime Day { get; private set; }
        protected bool IsToday { get; private set; }
        protected string PrevDayLink { get; private set; }
        protected string NextDayLink { get; private set; }

        // One line in the ledger: a payment at drop-off, or the refund when a job is voided.
        protected class Line
        {
            public DateTime At;
            public string Claim, Customer, What, Method, Staff;
            public decimal Amount;
            public bool IsRefund;
        }

        private List<SalesJob> dayJobs;
        protected List<Line> Lines { get; private set; }

        protected decimal CashIn, CashRefunds, EwalletIn, EwalletRefunds;
        protected decimal NetCash { get { return CashIn - CashRefunds; } }
        protected decimal NetEwallet { get { return EwalletIn - EwalletRefunds; } }
        protected decimal NetTotal { get { return NetCash + NetEwallet; } }
        protected int Orders { get; private set; }
        protected int Voids { get; private set; }
        protected decimal Kilos { get; private set; }
        protected decimal LastWeekTotal { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            DateTime d;
            if (!DateTime.TryParseExact(Request.QueryString["d"], "yyyy-MM-dd", Inv, DateTimeStyles.None, out d))
                d = SampleSales.Now.Date;
            Day = d < SampleSales.Opened ? SampleSales.Opened : d > SampleSales.Now.Date ? SampleSales.Now.Date : d;
            IsToday = Day == SampleSales.Now.Date;
            PrevDayLink = Day > SampleSales.Opened ? "DailySalesReport.aspx?d=" + Day.AddDays(-1).ToString("yyyy-MM-dd", Inv) : null;
            NextDayLink = !IsToday ? "DailySalesReport.aspx?d=" + Day.AddDays(1).ToString("yyyy-MM-dd", Inv) : null;

            if (!IsPostBack)
            {
                txtDate.Text = Day.ToString("yyyy-MM-dd", Inv);
                txtDate.Attributes["min"] = SampleSales.Opened.ToString("yyyy-MM-dd", Inv);
                txtDate.Attributes["max"] = SampleSales.Now.ToString("yyyy-MM-dd", Inv);
            }

            // The date goes in the title so a saved PDF or a browser tab says which day it is.
            // (Set here, not with a code block in <head>, because BasePage adds to the header.)
            Title = "Daily sales report · " + Day.ToString("MMM d, yyyy", Inv) + " · LabaTrack";

            Build();
        }

        protected void btnShow_Click(object sender, EventArgs e)
        {
            Response.Redirect("DailySalesReport.aspx?d=" + HttpUtility.UrlEncode(txtDate.Text), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void Build()
        {
            DateTime next = Day.AddDays(1);
            dayJobs = SampleSales.Jobs.Where(j => j.DroppedOff >= Day && j.DroppedOff < next).OrderBy(j => j.DroppedOff).ToList();

            Lines = new List<Line>();
            int seq = 0;
            foreach (var job in dayJobs)
            {
                seq++;
                string claim = "LT-" + Day.ToString("yyMMdd", Inv) + "-" + seq.ToString("000", Inv);
                string customer = SampleSales.Customers[job.CustomerId - 1].Name;
                string what = job.Service + " · " + job.WeightKg.ToString("0.0", Inv) + " kg";
                Lines.Add(new Line { At = job.DroppedOff, Claim = claim, Customer = customer, What = what, Method = job.Method, Staff = job.Staff, Amount = job.Total });
                if (job.Voided)
                    // DRAFT: the sample history has no void time, so the refund is put 15 minutes later.
                    Lines.Add(new Line { At = job.DroppedOff.AddMinutes(15), Claim = claim, Customer = customer, What = "Refund on void · " + job.VoidReason,
                                         Method = job.Method, Staff = job.Staff, Amount = -job.Total, IsRefund = true });
            }
            Lines = Lines.OrderBy(l => l.At).ToList();

            CashIn = Lines.Where(l => !l.IsRefund && l.Method == "Cash").Sum(l => l.Amount);
            CashRefunds = -Lines.Where(l => l.IsRefund && l.Method == "Cash").Sum(l => l.Amount);
            EwalletIn = Lines.Where(l => !l.IsRefund && l.Method == "E-wallet").Sum(l => l.Amount);
            EwalletRefunds = -Lines.Where(l => l.IsRefund && l.Method == "E-wallet").Sum(l => l.Amount);
            Orders = dayJobs.Count(j => !j.Voided);
            Voids = dayJobs.Count(j => j.Voided);
            Kilos = dayJobs.Where(j => !j.Voided).Sum(j => j.WeightKg);

            // Same weekday last week, up to the same time of day, so a morning is not compared with a whole day.
            DateTime cut = IsToday ? SampleSales.Now : next;
            LastWeekTotal = SampleSales.Jobs.Where(j => !j.Voided && j.DroppedOff >= Day.AddDays(-7) && j.DroppedOff < cut.AddDays(-7)).Sum(j => j.Total);
        }

        // ================================================================ Pieces for the markup

        protected string DayTitle { get { return Day.ToString("dddd, d MMMM yyyy", Inv); } }
        protected string ShopTitle { get { return ShopName; } }
        protected string PreparedAt { get { return SampleSales.Now.ToString("MMM d, yyyy h:mm tt", Inv); } }
        protected string Prepared { get { return PreparedAt + " by " + PreparedBy; } }
        protected string ReportNo { get { return "DSR-" + Day.ToString("yyyyMMdd", Inv); } }

        protected static string Peso(decimal v)
        {
            return (v < 0 ? "−₱" : "₱") + Math.Abs(v).ToString("N2", Inv);
        }

        protected string AveragePerOrder()
        {
            return Orders == 0 ? "—" : Peso(Math.Round(NetTotal / Orders, 2));
        }

        protected string VersusLastWeek()
        {
            string label = "vs " + Day.AddDays(-7).ToString("ddd MMM d", Inv) + (IsToday ? " at the same time" : "");
            if (LastWeekTotal == 0) return "No sales on " + Day.AddDays(-7).ToString("MMM d", Inv);
            decimal pct = (NetTotal - LastWeekTotal) / LastWeekTotal * 100;
            return (pct >= 0 ? "▲ " : "▼ ") + Math.Abs(pct).ToString("0.0", Inv) + "% " + label + " (" + Peso(LastWeekTotal) + ")";
        }

        /// <summary>Net payments per hour, cash and e-wallet stacked, sized to the A4 sheet.</summary>
        protected string HourChart()
        {
            var hours = Enumerable.Range(7, 14).ToList();
            var live = dayJobs.Where(j => !j.Voided).ToList();
            Func<string, double[]> by = method => hours.Select(h => (double)live.Where(j => j.Method == method && j.DroppedOff.Hour == h).Sum(j => j.Total)).ToArray();
            var series = new List<ChartSeries>
            {
                new ChartSeries("Cash", SvgChart.Blue, by("Cash")),
                new ChartSeries("E-wallet", SvgChart.Orange, by("E-wallet")),
            };
            string[] labels = hours.Select(h => new DateTime(2000, 1, 1, h, 0, 0).ToString("h tt", Inv).Replace(" ", "").ToLowerInvariant()).ToArray();
            string[] tips = hours.Select(h => new DateTime(2000, 1, 1, h, 0, 0).ToString("h:mm tt", Inv) + " – " + new DateTime(2000, 1, 1, h + 1, 0, 0).ToString("h:mm tt", Inv)).ToArray();
            return SvgChart.Legend(series)
                 + SvgChart.Columns("Payments by hour, cash and e-wallet", labels, tips, series, SvgChart.PesoShort, 150, 1, null, -1, true, 690, SvgChart.Peso);
        }

        protected string ServiceRows()
        {
            var live = dayJobs.Where(j => !j.Voided).ToList();
            var sb = new StringBuilder();
            foreach (var s in SampleSales.Services)
            {
                var list = live.Where(j => j.Service == s).ToList();
                sb.Append("<tr><td>").Append(HttpUtility.HtmlEncode(s)).Append("</td><td class=\"right num\">").Append(list.Count)
                  .Append("</td><td class=\"right num\">").Append(list.Sum(j => j.WeightKg).ToString("0.0", Inv))
                  .Append(" kg</td><td class=\"right num\">").Append(Peso(list.Sum(j => j.LaundryCharge))).Append("</td></tr>");
            }
            decimal services = live.SelectMany(j => j.AddOns).Where(a => a.Name == "Extra rinse" || a.Name == "Stain treatment").Sum(a => a.Amount);
            decimal products = live.SelectMany(j => j.AddOns).Where(a => a.Name == "Fabric conditioner" || a.Name == "Detergent sachet").Sum(a => a.Amount);
            sb.Append("<tr><td>Service add-ons</td><td></td><td></td><td class=\"right num\">").Append(Peso(services)).Append("</td></tr>");
            sb.Append("<tr><td>Product add-ons</td><td></td><td></td><td class=\"right num\">").Append(Peso(products)).Append("</td></tr>");
            return sb.ToString();
        }

        protected string StaffRows()
        {
            var sb = new StringBuilder();
            foreach (var s in SampleSales.Staff)
            {
                var list = dayJobs.Where(j => j.Staff == s).ToList();
                if (list.Count == 0) continue;
                sb.Append("<tr><td>").Append(HttpUtility.HtmlEncode(s)).Append("</td><td class=\"right num\">").Append(list.Count(j => !j.Voided))
                  .Append("</td><td class=\"right num\">").Append(list.Count(j => j.Voided))
                  .Append("</td><td class=\"right num\">").Append(Peso(list.Where(j => !j.Voided).Sum(j => j.Total))).Append("</td></tr>");
            }
            return sb.Length == 0 ? "<tr><td colspan=\"4\" class=\"muted\">No orders taken.</td></tr>" : sb.ToString();
        }

        protected string LedgerRows()
        {
            if (Lines.Count == 0)
                return "<tr><td colspan=\"7\" class=\"muted\">No payments on this day.</td></tr>";
            var sb = new StringBuilder();
            foreach (var l in Lines)
            {
                sb.Append(l.IsRefund ? "<tr class=\"refund\">" : "<tr>")
                  .Append("<td class=\"num\">").Append(l.At.ToString("h:mm tt", Inv)).Append("</td>")
                  .Append("<td class=\"mono\">").Append(HttpUtility.HtmlEncode(l.Claim)).Append("</td>")
                  .Append("<td>").Append(HttpUtility.HtmlEncode(l.Customer)).Append("</td>")
                  .Append("<td class=\"what\">").Append(HttpUtility.HtmlEncode(l.What)).Append("</td>")
                  .Append("<td>").Append(HttpUtility.HtmlEncode(l.Method)).Append("</td>")
                  .Append("<td>").Append(HttpUtility.HtmlEncode(l.Staff)).Append("</td>")
                  .Append("<td class=\"right num\">").Append(Peso(l.Amount)).Append("</td></tr>");
            }
            return sb.ToString();
        }
    }
}

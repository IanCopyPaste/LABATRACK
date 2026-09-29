using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using LABATRACK.DataAccess;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Admin
{
    // One day of payments for the drawer check (?d=yyyy-MM-dd, default today), from
    // ReportService.GetDailyReport. Money is counted on Payments.PaidAt, within the half-open day
    // [midnight, next midnight), so the net total always equals, checked by hand:
    //     SELECT SUM(Amount) FROM Payments WHERE PaidAt >= @day AND PaidAt < @nextDay
    // The job counts (orders, voids, kilos, by service, by staff) are for the jobs dropped off
    // that day.
    //
    // The page is laid out as one A4 sheet. On screen it is shown as a sheet of paper with a
    // toolbar above it; printing drops the toolbar and prints the sheet as it is, with its own
    // page margins and page numbers, rather than a copy of the screen.
    public partial class DailySalesReport : AdminBasePage
    {
        protected override string PageStyleSheet
        {
            get { return "DailySalesReport.css"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected DateTime Day { get; private set; }
        protected bool IsToday { get; private set; }
        protected string PrevDayLink { get; private set; }
        protected string NextDayLink { get; private set; }

        // One line in the ledger: a payment at drop-off (in full or a down payment), a balance
        // paid later, a correction, or a refund.
        protected class Line
        {
            public DateTime At;
            public string Claim, Customer, What, Method, Staff;
            public decimal Amount;
            public bool IsRefund;
        }

        protected List<Line> Lines { get; private set; }

        private ReportService.DailyReport report;
        private List<JobOrder> liveJobs;   // dropped off that day and not voided
        private DateTime now;
        private string shopName;

        protected decimal CashIn { get { return report.Sales.CashIn; } }
        protected decimal CashRefunds { get { return report.Sales.CashRefunds; } }
        protected decimal EwalletIn { get { return report.Sales.EwalletIn; } }
        protected decimal EwalletRefunds { get { return report.Sales.EwalletRefunds; } }
        protected decimal NetCash { get { return report.Sales.NetCash; } }
        protected decimal NetEwallet { get { return report.Sales.NetEwallet; } }
        protected decimal NetTotal { get { return report.Sales.NetTotal; } }
        protected int Orders { get { return liveJobs.Count; } }
        protected int Voids { get { return report.Jobs.Count - liveJobs.Count; } }
        protected decimal Kilos { get { return liveJobs.Sum(j => j.WeightKg); } }
        protected decimal LastWeekTotal { get { return report.SameDayLastWeek.NetTotal; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            now = DateTime.Now;
            DateTime first = ReportService.FirstDay(now);
            DateTime d;
            if (!DateTime.TryParseExact(Request.QueryString["d"], "yyyy-MM-dd", Inv, DateTimeStyles.None, out d))
                d = now.Date;
            Day = d < first ? first : d > now.Date ? now.Date : d;
            IsToday = Day == now.Date;
            PrevDayLink = Day > first ? "DailySalesReport.aspx?d=" + Day.AddDays(-1).ToString("yyyy-MM-dd", Inv) : null;
            NextDayLink = !IsToday ? "DailySalesReport.aspx?d=" + Day.AddDays(1).ToString("yyyy-MM-dd", Inv) : null;

            if (!IsPostBack)
            {
                txtDate.Text = Day.ToString("yyyy-MM-dd", Inv);
                txtDate.Attributes["min"] = first.ToString("yyyy-MM-dd", Inv);
                txtDate.Attributes["max"] = now.ToString("yyyy-MM-dd", Inv);
            }

            // The date goes in the title so a saved PDF or a browser tab says which day it is.
            // (Set here, not with a code block in <head>, because BasePage adds to the header.)
            Title = "Daily sales report · " + Day.ToString("MMM d, yyyy", Inv) + " · LabaTrack";

            shopName = SettingsRepository.Get().ShopName;
            report = ReportService.GetDailyReport(Day, now);
            liveJobs = report.Jobs.Where(j => j.CurrentStatus != StatusService.Voided).ToList();
            Lines = report.Ledger.Select(ToLine).ToList();
        }

        protected void btnShow_Click(object sender, EventArgs e)
        {
            Response.Redirect("DailySalesReport.aspx?d=" + HttpUtility.UrlEncode(txtDate.Text), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static Line ToLine(Payment p)
        {
            string what;
            if (p.Kind == Payment.VoidRefund)
                what = "Refund on void" + (string.IsNullOrEmpty(p.Reason) ? "" : " · " + p.Reason);
            else if (p.Kind == Payment.Adjustment)
                what = (p.Amount > 0 ? "Correction, collected" : "Correction, refunded") + " · " + p.ServiceName;
            else if (p.Kind == Payment.BalancePayment)
                what = "Balance paid · " + p.ServiceName;
            else
                what = (p.Amount < p.JobTotal ? "Down payment · " : "") + p.ServiceName + " · " + p.WeightKg.ToString("0.0#", Inv) + " kg";
            return new Line
            {
                At = p.PaidAt, Claim = p.ClaimNumber, Customer = p.CustomerName, What = what,
                Method = p.Method, Staff = p.RecordedBy, Amount = p.Amount, IsRefund = p.Amount < 0,
            };
        }

        // ================================================================ Pieces for the markup

        protected string DayTitle { get { return Day.ToString("dddd, d MMMM yyyy", Inv); } }
        protected string ShopTitle { get { return shopName; } }
        protected string PreparedAt { get { return now.ToString("MMM d, yyyy h:mm tt", Inv); } }
        protected string Prepared { get { return PreparedAt + " by " + CurrentUser.Username; } }
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
            // The shop's hours, 7 AM to 8 PM, stretched if a payment came in outside them.
            int firstHour = Math.Min(7, Lines.Count == 0 ? 7 : Lines.Min(l => l.At.Hour));
            int lastHour = Math.Max(20, Lines.Count == 0 ? 20 : Lines.Max(l => l.At.Hour));
            var hours = Enumerable.Range(firstHour, lastHour - firstHour + 1).ToList();
            Func<string, double[]> by = method => hours.Select(h => Math.Max(0, (double)Lines.Where(l => l.Method == method && l.At.Hour == h).Sum(l => l.Amount))).ToArray();
            var series = new List<ChartSeries>
            {
                new ChartSeries("Cash", SvgChart.Blue, by(Payment.Cash)),
                new ChartSeries("E-wallet", SvgChart.Orange, by(Payment.Ewallet)),
            };
            string[] labels = hours.Select(h => new DateTime(2000, 1, 1, h, 0, 0).ToString("h tt", Inv).Replace(" ", "").ToLowerInvariant()).ToArray();
            string[] tips = hours.Select(h => new DateTime(2000, 1, 1, h, 0, 0).ToString("h:mm tt", Inv) + " – " + new DateTime(2000, 1, 1, h, 0, 0).AddHours(1).ToString("h:mm tt", Inv)).ToArray();
            return SvgChart.Legend(series)
                 + SvgChart.Columns("Payments by hour, cash and e-wallet", labels, tips, series, SvgChart.PesoShort, 150, 1, null, -1, true, 690, SvgChart.Peso);
        }

        protected string ServiceRows()
        {
            var sb = new StringBuilder();
            foreach (var g in liveJobs.GroupBy(j => j.ServiceName).OrderByDescending(g => g.Count()))
            {
                sb.Append("<tr><td>").Append(HttpUtility.HtmlEncode(g.Key)).Append("</td><td class=\"right num\">").Append(g.Count())
                  .Append("</td><td class=\"right num\">").Append(g.Sum(j => j.WeightKg).ToString("0.0", Inv))
                  .Append(" kg</td><td class=\"right num\">").Append(Peso(g.Sum(j => j.LaundryCharge))).Append("</td></tr>");
            }
            if (sb.Length == 0)
                sb.Append("<tr><td colspan=\"4\" class=\"muted\">No job orders taken.</td></tr>");
            sb.Append("<tr><td>Service add-ons</td><td></td><td></td><td class=\"right num\">").Append(Peso(liveJobs.Sum(j => j.ServiceAddOnAmount))).Append("</td></tr>");
            sb.Append("<tr><td>Product add-ons</td><td></td><td></td><td class=\"right num\">").Append(Peso(liveJobs.Sum(j => j.ProductAddOnAmount))).Append("</td></tr>");
            return sb.ToString();
        }

        protected string StaffRows()
        {
            var sb = new StringBuilder();
            foreach (var g in report.Jobs.GroupBy(j => j.CreatedByName).OrderBy(g => g.Key))
            {
                var live = g.Where(j => j.CurrentStatus != StatusService.Voided).ToList();
                sb.Append("<tr><td>").Append(HttpUtility.HtmlEncode(g.Key)).Append("</td><td class=\"right num\">").Append(live.Count)
                  .Append("</td><td class=\"right num\">").Append(g.Count() - live.Count)
                  .Append("</td><td class=\"right num\">").Append(Peso(live.Sum(j => j.TotalAmount))).Append("</td></tr>");
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

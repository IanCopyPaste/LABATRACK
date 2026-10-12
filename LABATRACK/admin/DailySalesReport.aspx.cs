using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using LABATRACK.db_conn;
using LABATRACK.tools;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.admin
{
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

        protected class Line
        {
            public DateTime At;
            public string Claim, Customer, What, Method, Staff;
            public decimal Amount;
            public bool IsRefund;
        }

        protected List<Line> Lines { get; private set; }

        private DaySales sales;
        private DaySales lastWeek;
        private List<Payment> ledger;
        private List<JobOrder> allJobs;
        private List<JobOrder> liveJobs;
        private DateTime now;
        private string shopName;

        protected decimal CashIn { get { return sales.CashIn; } }
        protected decimal CashRefunds { get { return sales.CashRefunds; } }
        protected decimal EwalletIn { get { return sales.EwalletIn; } }
        protected decimal EwalletRefunds { get { return sales.EwalletRefunds; } }
        protected decimal NetCash { get { return sales.NetCash; } }
        protected decimal NetEwallet { get { return sales.NetEwallet; } }
        protected decimal NetTotal { get { return sales.NetTotal; } }
        protected int Orders { get { return liveJobs.Count; } }
        protected int Voids { get { return allJobs.Count - liveJobs.Count; } }
        protected decimal Kilos { get { return liveJobs.Sum(j => j.WeightKg); } }
        protected decimal LastWeekTotal { get { return lastWeek.NetTotal; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            now = DateTime.Now;
            DateTime first = FirstDay();
            DateTime d;
            if (!DateTime.TryParseExact(Request.QueryString["d"], "yyyy-MM-dd", Inv, DateTimeStyles.None, out d))
                d = now.Date;
            Day = d < first ? first : d > now.Date ? now.Date : d;
            IsToday = Day == now.Date;
            if (Day > first)
                PrevDayLink = "DailySalesReport.aspx?d=" + Day.AddDays(-1).ToString("yyyy-MM-dd", Inv);
            else
                PrevDayLink = null;
            if (!IsToday)
                NextDayLink = "DailySalesReport.aspx?d=" + Day.AddDays(1).ToString("yyyy-MM-dd", Inv);
            else
                NextDayLink = null;

            if (!IsPostBack)
            {
                txtDate.Text = Day.ToString("yyyy-MM-dd", Inv);
                txtDate.Attributes["min"] = first.ToString("yyyy-MM-dd", Inv);
                txtDate.Attributes["max"] = now.ToString("yyyy-MM-dd", Inv);
            }

            Title = "Daily sales report · " + Day.ToString("MMM d, yyyy", Inv) + " · LabaTrack";

            shopName = ReadShopName();

            DateTime from = Day.Date;
            DateTime to = from.AddDays(1);
            DateTime cutoff;
            if (now < to)
                cutoff = now;
            else
                cutoff = to;
            sales = ReadSales(from, to);
            lastWeek = ReadSales(from.AddDays(-7), cutoff.AddDays(-7));

            ledger = new List<Payment>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetBetween", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Payment payment = new Payment();
                        payment.PaymentId = (int)r["PaymentId"];
                        payment.JobId = (int)r["JobId"];
                        payment.Kind = (string)r["Kind"];
                        payment.Amount = (decimal)r["Amount"];
                        payment.Method = (string)r["Method"];
                        payment.CashReceived = r["CashReceived"] as decimal?;
                        payment.PaidAt = (DateTime)r["PaidAt"];
                        payment.RecordedByUserId = (int)r["RecordedByUserId"];
                        payment.RecordedBy = (string)r["RecordedBy"];
                        payment.ClaimNumber = (string)r["ClaimNumber"];
                        payment.CustomerName = (string)r["CustomerName"];
                        payment.ServiceName = (string)r["ServiceName"];
                        payment.WeightKg = (decimal)r["WeightKg"];
                        payment.JobTotal = (decimal)r["JobTotal"];
                        payment.Reason = r["Reason"] as string;
                        ledger.Add(payment);
                    }
                }
            }

            allJobs = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetCreatedBetween", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        allJobs.Add(JobOrder.FromRow(r));
                    }
                }
            }
            liveJobs = allJobs.Where(j => j.CurrentStatus != JobStatus.Voided).ToList();
            Lines = ledger.Select(ToLine).ToList();
        }

        private DateTime FirstDay()
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Reports_GetFirstJobDate", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                object first = cmd.ExecuteScalar();
                if (first != null && first != DBNull.Value && (DateTime)first < now)
                    return ((DateTime)first).Date;
                return now.Date;
            }
        }

        private string ReadShopName()
        {
            string name = new ShopSettings().ShopName;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        if ((string)r["SettingKey"] == ShopSettings.ShopNameKey && r["SettingValue"] as string != null)
                            name = (string)r["SettingValue"];
                    }
                }
            }
            return name;
        }

        private DaySales ReadSales(DateTime from, DateTime to)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetDaySummary", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    r.Read();

                    DaySales result = new DaySales();
                    result.CashIn = Convert.ToDecimal(r["CashIn"]);
                    result.CashRefunds = Convert.ToDecimal(r["CashRefunds"]);
                    result.EwalletIn = Convert.ToDecimal(r["EwalletIn"]);
                    result.EwalletRefunds = Convert.ToDecimal(r["EwalletRefunds"]);
                    result.PaymentCount = Convert.ToInt32(r["PaymentCount"]);
                    result.RefundCount = Convert.ToInt32(r["RefundCount"]);
                    return result;
                }
            }
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
            if (Orders == 0)
                return "—";
            else
                return Peso(Math.Round(NetTotal / Orders, 2));
        }

        protected string VersusLastWeek()
        {
            string label = "vs " + Day.AddDays(-7).ToString("ddd MMM d", Inv) + (IsToday ? " at the same time" : "");
            if (LastWeekTotal == 0) return "No sales on " + Day.AddDays(-7).ToString("MMM d", Inv);
            decimal pct = (NetTotal - LastWeekTotal) / LastWeekTotal * 100;
            return (pct >= 0 ? "▲ " : "▼ ") + Math.Abs(pct).ToString("0.0", Inv) + "% " + label + " (" + Peso(LastWeekTotal) + ")";
        }

        protected string HourChart()
        {
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
            foreach (var g in allJobs.GroupBy(j => j.CreatedByName).OrderBy(g => g.Key))
            {
                var live = g.Where(j => j.CurrentStatus != JobStatus.Voided).ToList();
                sb.Append("<tr><td>").Append(HttpUtility.HtmlEncode(g.Key)).Append("</td><td class=\"right num\">").Append(live.Count)
                  .Append("</td><td class=\"right num\">").Append(g.Count() - live.Count)
                  .Append("</td><td class=\"right num\">").Append(Peso(live.Sum(j => j.TotalAmount))).Append("</td></tr>");
            }
            if (sb.Length == 0)
                return "<tr><td colspan=\"4\" class=\"muted\">No orders taken.</td></tr>";
            else
                return sb.ToString();
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

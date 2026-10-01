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
    public partial class Dashboard : AdminBasePage
    {
        protected override string ActiveNav
        {
            get { return "Dashboard"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected DashboardData Data { get; private set; }

        protected string Greeting
        {
            get
            {
                int hour = Data.Now.Hour;
                string part = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
                return part + ", " + CurrentUser.Username;
            }
        }

        protected string CashShare { get { return Share(Data.Today.NetCash); } }
        protected string EwalletShare { get { return Share(Data.Today.NetEwallet); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadDashboard();

            rptActive.DataSource = Data.Active.OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.JobId);
            rptActive.DataBind();
            rptUnclaimed.DataSource = Data.Unclaimed;
            rptUnclaimed.DataBind();
            rptVoided.DataSource = Data.VoidedToday;
            rptVoided.DataBind();
            rptRefunds.DataSource = Data.RefundsWaitingEarlier;
            rptRefunds.DataBind();
            rptClaimed.DataSource = Data.ClaimedToday;
            rptClaimed.DataBind();
        }

        private void LoadDashboard()
        {
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime tomorrow = today.AddDays(1);

            int days = 7;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string value = r["SettingValue"] as string;
                        int number;
                        if ((string)r["SettingKey"] == ShopSettings.UnclaimedThresholdDaysKey && value != null
                            && int.TryParse(value, NumberStyles.Integer, Inv, out number))
                            days = number;
                    }
                }
            }
            days = Math.Max(1, days);

            Data = new DashboardData();
            Data.Now = now;
            Data.UnclaimedDays = days;

            Data.Active = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetActive", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Data.Active.Add(JobOrder.FromRow(r));
                    }
                }
            }

            Data.Today = new DaySales();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetDaySummary", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = today;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = tomorrow;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    r.Read();
                    Data.Today.CashIn = Convert.ToDecimal(r["CashIn"]);
                    Data.Today.CashRefunds = Convert.ToDecimal(r["CashRefunds"]);
                    Data.Today.EwalletIn = Convert.ToDecimal(r["EwalletIn"]);
                    Data.Today.EwalletRefunds = Convert.ToDecimal(r["EwalletRefunds"]);
                    Data.Today.PaymentCount = Convert.ToInt32(r["PaymentCount"]);
                    Data.Today.RefundCount = Convert.ToInt32(r["RefundCount"]);
                }
            }

            Data.ClaimedToday = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetClosedBetween", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = JobStatus.Claimed;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = today;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = tomorrow;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Data.ClaimedToday.Add(JobOrder.FromRow(r));
                    }
                }
            }

            Data.VoidedToday = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetClosedBetween", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = JobStatus.Voided;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = today;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = tomorrow;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Data.VoidedToday.Add(JobOrder.FromRow(r));
                    }
                }
            }

            Data.Unclaimed = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetReadyBefore", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ReadyBefore", SqlDbType.DateTime2).Value = today.AddDays(1 - days);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Data.Unclaimed.Add(JobOrder.FromRow(r));
                    }
                }
            }

            Data.RefundsWaiting = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetPendingRefunds", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Data.RefundsWaiting.Add(JobOrder.FromRow(r));
                    }
                }
            }
        }

        private string Share(decimal part)
        {
            decimal whole = Math.Max(0, Data.Today.NetCash) + Math.Max(0, Data.Today.NetEwallet);
            decimal pct;
            if (whole <= 0)
                pct = 0;
            else
                pct = Math.Max(0, part) / whole * 100;
            return pct.ToString("0.#", Inv);
        }

        protected string StageOptions()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<option value=\"\">All stages (").Append(Data.Active.Count).Append(")</option>");
            foreach (string stage in JobStatus.BoardStages)
            {
                string encoded = HttpUtility.HtmlAttributeEncode(stage);
                sb.Append("<option value=\"").Append(encoded).Append("\">").Append(HttpUtility.HtmlEncode(stage))
                  .Append(" (").Append(Data.CountAt(stage)).Append(")</option>");
            }
            return sb.ToString();
        }

        protected decimal BalancesDue
        {
            get { return Data.Active.Sum(j => j.Balance); }
        }

        protected int LoadsOwing
        {
            get { return Data.Active.Count(j => j.Balance > 0); }
        }

        protected string UnclaimedNote(JobOrder job)
        {
            int days = (Data.Now.Date - job.StatusSince.Date).Days;
            return "Ready since " + job.StatusSince.ToString("MMM d", Inv) + " · " + days + (days == 1 ? " day" : " days") + " unclaimed · "
                 + (job.Balance > 0 ? Peso(job.Balance) + " still to pay" : "paid");
        }

        protected static string VoidNote(JobOrder job)
        {
            return RefundNotes.ForVoid(job);
        }

        protected string RefundNote(JobOrder job)
        {
            return RefundNotes.Waiting(job, Data.Now);
        }

        protected static string PaymentBadgeFor(JobOrder job)
        {
            return PaymentBadge.Html(job);
        }

        protected static string Peso(decimal amount)
        {
            return (amount < 0 ? "−₱" : "₱") + Math.Abs(amount).ToString("N2", Inv);
        }

        protected static string StageCss(string stage)
        {
            switch (stage)
            {
                case "Queued": return "st-queued";
                case "Washing": return "st-washing";
                case "Drying": return "st-drying";
                case "Folding": return "st-folding";
                case "Inspection": return "st-inspection";
                case "Ready for pick-up": return "st-ready";
                case "Voided": return "st-voided";
                default: return "st-claimed";
            }
        }
    }
}

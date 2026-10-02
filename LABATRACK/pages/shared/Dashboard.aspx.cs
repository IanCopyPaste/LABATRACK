using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.db_conn;
using LABATRACK.tools;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.pages.shared
{
    public partial class Dashboard : BasePage
    {
        protected override string ActiveNav
        {
            get { return "Board"; }
        }

        protected override string PageStyleSheet
        {
            get { return "Dashboard.css"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected DashboardData Data { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadDashboard();
            if (!IsPostBack)
                Bind();
        }

        protected void Cards_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string[] parts = ((string)e.CommandArgument).Split('|');
            int jobId;
            if (e.CommandName != "Move" || parts.Length != 2 || !int.TryParse(parts[0], out jobId)) return;
            string expectedStatus = parts[1];

            JobOrder shown = Data.Active.FirstOrDefault(j => j.JobId == jobId);
            string claim;
            if (shown != null)
                claim = shown.ClaimNumber;
            else
                claim = "The job";

            string error = null;
            string to = null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = null;
                    using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetForUpdate", conn, tx))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                                job = JobOrder.FromRow(r);
                        }
                    }

                    if (job == null)
                    {
                        error = "That job could not be found.";
                    }
                    else if (job.CurrentStatus != expectedStatus)
                    {
                        error = JobStatus.AlreadyUpdated;
                    }
                    else
                    {
                        to = job.NextStage;
                        decimal balance = 0m;

                        if (to == null)
                        {
                            error = "This job is " + job.CurrentStatus.ToLowerInvariant() + " and cannot move any more.";
                        }
                        else
                        {
                            if (to == JobStatus.Claimed)
                            {
                                using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetJobTotals", conn, tx))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                                    using (SqlDataReader r = cmd.ExecuteReader())
                                    {
                                        if (r.Read())
                                            balance = (decimal)r["TotalAmount"] - (decimal)r["AmountPaid"];
                                    }
                                }
                            }

                            if (balance > 0)
                            {
                                error = "This load still has a " + Peso(balance)
                                      + " balance. Collect it on the job page before handing the load over.";
                            }
                            else
                            {
                                int rowsAffected;
                                using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_UpdateStatus", conn, tx))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                                    cmd.Parameters.Add("@ExpectedStatus", SqlDbType.NVarChar, 20).Value = job.CurrentStatus;
                                    cmd.Parameters.Add("@NewStatus", SqlDbType.NVarChar, 20).Value = to;

                                    rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                                }

                                if (rowsAffected != 1)
                                {
                                    error = JobStatus.AlreadyUpdated;
                                }
                                else
                                {
                                    using (SqlCommand cmd = new SqlCommand("dbo.usp_JobStatusHistory_Insert", conn, tx))
                                    {
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                                        cmd.Parameters.Add("@FromStatus", SqlDbType.NVarChar, 20).Value = job.CurrentStatus;
                                        cmd.Parameters.Add("@ToStatus", SqlDbType.NVarChar, 20).Value = to;
                                        cmd.Parameters.Add("@ChangedByUserId", SqlDbType.Int).Value = CurrentUser.UserId;
                                        cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = DBNull.Value;

                                        cmd.ExecuteNonQuery();
                                    }

                                    tx.Commit();
                                }
                            }
                        }
                    }
                }
            }

            if (error != null)
            {
                ShowNotice("warn", error);
            }
            else
            {
                if (to == JobStatus.Ready)
                    Email.SendReady(jobId, CurrentUser.UserId, false);

                ShowNotice("info", to == JobStatus.Claimed ? claim + " was handed over (claimed)." : claim + " moved to " + to + ".");
            }

            LoadDashboard();
            Bind();
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

        private void Bind()
        {
            rptColumns.DataSource = JobStatus.BoardStages.Select(stage => new BoardColumn
            {
                Stage = stage,
                Jobs = Data.Active.Where(j => j.CurrentStatus == stage).ToList(),
            });
            rptColumns.DataBind();

            rptClaimed.DataSource = Data.ClaimedToday;
            rptClaimed.DataBind();
            rptUnclaimed.DataSource = Data.Unclaimed;
            rptUnclaimed.DataBind();
            rptVoided.DataSource = Data.VoidedToday;
            rptVoided.DataBind();
            rptRefunds.DataSource = Data.RefundsWaitingEarlier;
            rptRefunds.DataBind();
        }

        private void ShowNotice(string kind, string text)
        {
            string icon;
            if (kind == "warn")
                icon = "circle-alert";
            else
                icon = "circle-check";
            litNotice.Text = "<div class=\"notice notice-" + kind + "\">" + Icons.Get(icon, "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        protected static string ActionLabel(JobOrder job)
        {
            string next = job.NextStage;
            bool startingWork = job.CurrentStatus == JobStatus.IntakeInspection;
            switch (next)
            {
                case JobStatus.IntakeInspection: return "Start intake inspection";
                case JobStatus.Washing: return "Start washing";
                case JobStatus.Drying:
                    if (startingWork) return "Start drying";
                    return "Move to drying";
                case JobStatus.Folding:
                    if (startingWork) return "Start folding";
                    return "Move to folding";
                case JobStatus.Inspection: return "Send to inspection";
                case JobStatus.Ready: return "Pass";
                default: return "Mark claimed";
            }
        }

        protected static bool CollectFirst(JobOrder job)
        {
            return job.CurrentStatus == JobStatus.Ready && job.Balance > 0;
        }

        protected static string RouteNote(JobOrder job)
        {
            List<string> skipped = new List<string>();
            if (!job.IncludesWashing) skipped.Add("washing");
            if (!job.IncludesDrying) skipped.Add("drying");
            if (!job.IncludesFolding) skipped.Add("folding");
            if (skipped.Count == 0)
                return "";
            else
                return "No " + string.Join(" or ", skipped);
        }

        protected static string ReworkNote(JobOrder job)
        {
            bool sentBack = job.StatusFrom == JobStatus.Inspection && job.WorkStages.Contains(job.CurrentStatus);
            if (sentBack && !string.IsNullOrEmpty(job.StatusReason))
                return "Back from inspection: " + job.StatusReason;
            else
                return "";
        }

        protected static string SearchText(JobOrder job)
        {
            string contact = job.ContactNumber ?? "";
            return (job.ClaimNumber + " " + job.CustomerName + " " + contact + " " + contact.Replace(" ", "")).ToLowerInvariant();
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

        protected static string StageCss(string stage)
        {
            switch (stage)
            {
                case "Queued": return "st-queued";
                case "Intake inspection": return "st-intake";
                case "Washing": return "st-washing";
                case "Drying": return "st-drying";
                case "Folding": return "st-folding";
                case "Inspection": return "st-inspection";
                case "Ready for pick-up": return "st-ready";
                case "Voided": return "st-voided";
                default: return "st-claimed";
            }
        }

        protected string When(DateTime at)
        {
            return (at.Date == Data.Now.Date ? "Today" : at.ToString("MMM d", Inv)) + ", " + at.ToString("h:mm tt", Inv);
        }

        protected string Age(DateTime droppedOff)
        {
            TimeSpan age = Data.Now - droppedOff;
            if (age.TotalHours < 1) return Math.Max(0, (int)age.TotalMinutes) + "m";
            if (age.TotalHours < 10) return (int)age.TotalHours + "h " + age.Minutes + "m";
            if (age.TotalDays < 1) return (int)age.TotalHours + "h";
            return (int)age.TotalDays + "d " + age.Hours + "h";
        }

        protected static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2", Inv);
        }

        public class BoardColumn
        {
            public string Stage { get; set; }
            public List<JobOrder> Jobs { get; set; }
        }
    }
}

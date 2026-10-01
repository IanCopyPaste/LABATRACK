using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using LABATRACK.db_conn;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.pages.shared
{
    public partial class History : BasePage
    {
        private const int PageSize = 10;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected override string ActiveNav
        {
            get { return "History"; }
        }

        protected override string PageStyleSheet
        {
            get { return "History.css"; }
        }

        protected int ResultCount { get; private set; }
        protected decimal ResultSales { get; private set; }
        protected int PageNumber { get; private set; }
        protected int PageCount { get; private set; }
        protected bool HasFilter { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            string q = (Request.QueryString["q"] ?? "").Trim();
            string status = Request.QueryString["status"] ?? "";
            DateTime? from = ParseDate(Request.QueryString["from"]);
            DateTime? to = ParseDate(Request.QueryString["to"]);

            if (!IsOwner && status == "Voided") status = "";
            if (status != "Active" && status != "Claimed" && status != "Voided") status = "";

            if (!IsPostBack)
            {
                txtSearch.Text = q;
                if (!IsOwner) ddlStatus.Items.Remove(ddlStatus.Items.FindByValue("Voided"));
                if (ddlStatus.Items.FindByValue(status) != null)
                    ddlStatus.SelectedValue = status;
                else
                    ddlStatus.SelectedValue = "";
                if (from.HasValue)
                    txtFrom.Text = from.Value.ToString("yyyy-MM-dd", Inv);
                else
                    txtFrom.Text = "";
                if (to.HasValue)
                    txtTo.Text = to.Value.ToString("yyyy-MM-dd", Inv);
                else
                    txtTo.Text = "";
            }

            HasFilter = q != "" || status != "" || from.HasValue || to.HasValue;

            object text = q == "" ? (object)DBNull.Value : q;
            string typedDigits = new string(q.Where(char.IsDigit).ToArray());
            object digits = typedDigits.Length < 4 ? (object)DBNull.Value : typedDigits;
            object statusFilter = status == "" ? (object)DBNull.Value : status;
            object fromFilter = from.HasValue ? (object)from.Value : DBNull.Value;
            object toFilter = to.HasValue ? (object)to.Value.AddDays(1) : DBNull.Value;

            int count = 0;
            decimal sales = 0m;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_SearchSummary", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 100).Value = text;
                cmd.Parameters.Add("@Digits", SqlDbType.NVarChar, 20).Value = digits;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = statusFilter;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = fromFilter;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = toFilter;
                cmd.Parameters.Add("@IncludeVoided", SqlDbType.Bit).Value = IsOwner;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    r.Read();
                    count = Convert.ToInt32(r["JobCount"]);
                    sales = Convert.ToDecimal(r["SalesTotal"]);
                }
            }
            ResultCount = count;
            ResultSales = sales;

            PageCount = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
            int page;
            if (int.TryParse(Request.QueryString["page"], out page))
                PageNumber = Math.Min(Math.Max(page, 1), PageCount);
            else
                PageNumber = 1;

            List<JobOrder> jobs = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_Search", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 100).Value = text;
                cmd.Parameters.Add("@Digits", SqlDbType.NVarChar, 20).Value = digits;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = statusFilter;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = fromFilter;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = toFilter;
                cmd.Parameters.Add("@IncludeVoided", SqlDbType.Bit).Value = IsOwner;
                cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (PageNumber - 1) * PageSize;
                cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = PageSize;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        jobs.Add(JobOrder.FromRow(r));
                    }
                }
            }

            rptJobs.DataSource = jobs;
            rptJobs.DataBind();
            pnlEmpty.Visible = count == 0;
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            Response.Redirect(Link(txtSearch.Text.Trim(), ddlStatus.SelectedValue, txtFrom.Text, txtTo.Text, 1), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static DateTime? ParseDate(string value)
        {
            DateTime d;
            return DateTime.TryParseExact(value, "yyyy-MM-dd", Inv, DateTimeStyles.None, out d)
                ? d : (DateTime?)null;
        }

        protected string PageLink(int page)
        {
            return Link(Request.QueryString["q"], Request.QueryString["status"], Request.QueryString["from"], Request.QueryString["to"], page);
        }

        private static string Link(string q, string status, string from, string to, int page)
        {
            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(q)) parts.Add("q=" + HttpUtility.UrlEncode(q));
            if (!string.IsNullOrEmpty(status)) parts.Add("status=" + HttpUtility.UrlEncode(status));
            if (!string.IsNullOrEmpty(from)) parts.Add("from=" + HttpUtility.UrlEncode(from));
            if (!string.IsNullOrEmpty(to)) parts.Add("to=" + HttpUtility.UrlEncode(to));
            if (page > 1) parts.Add("page=" + page);
            return "History.aspx" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
        }

        protected static string StatusNote(JobOrder job)
        {
            if (job.CurrentStatus == JobStatus.Claimed) return "Claimed " + When(job.StatusSince);
            if (job.CurrentStatus == JobStatus.Voided) return "Voided " + When(job.StatusSince);
            return "In the shop";
        }

        protected static string ClosedByNote(JobOrder job)
        {
            if (job.IsInShop) return "";
            bool voided = job.CurrentStatus == JobStatus.Voided;
            string html = "<div class=\"muted\">" + (voided ? "Voided" : "Handed over") + " by " + HttpUtility.HtmlEncode(job.StatusBy) + "</div>";
            if (voided && !string.IsNullOrEmpty(job.StatusReason))
                html += "<div class=\"void-reason\">&ldquo;" + HttpUtility.HtmlEncode(job.StatusReason) + "&rdquo;</div>";
            return html;
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

        protected static string When(DateTime at)
        {
            return (at.Date == DateTime.Today ? "Today" : at.ToString("MMM d", Inv)) + ", " + at.ToString("h:mm tt", Inv);
        }

        protected static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2", Inv);
        }
    }
}

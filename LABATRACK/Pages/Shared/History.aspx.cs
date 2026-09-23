using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using LABATRACK.Helpers;
using LABATRACK.Security;

namespace LABATRACK.Pages.Shared
{
    // DRAFT: past and current jobs from Helpers/SampleData. Phase 4 replaces SampleData with
    // a repository search (parameterized, filtered in SQL) that returns the same rows.
    //
    // The filters live in the query string, not in ViewState: Search redirects to
    // History.aspx?q=...&status=..., so a search can be bookmarked, the Back button works,
    // and refreshing never asks to resend a form.
    public partial class History : BasePage
    {
        private const int PageSize = 10;

        protected override string ActiveNav
        {
            get { return "History"; }
        }

        protected override string PageStyleSheet
        {
            get { return "History.css"; }
        }

        // DRAFT: Phase 2 takes the role from the signed-in account. Staff may search past jobs,
        // but voided jobs, who did what, and sales totals are the owner's (CLAUDE.md, Roles).
        protected bool IsOwner { get { return true; } }

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

            // Staff never see voided jobs, even by typing status=Voided into the address bar.
            if (!IsOwner && status == "Voided") status = "";

            if (!IsPostBack)
            {
                txtSearch.Text = q;
                ddlStatus.SelectedValue = ddlStatus.Items.FindByValue(status) != null ? status : "";
                txtFrom.Text = from.HasValue ? from.Value.ToString("yyyy-MM-dd") : "";
                txtTo.Text = to.HasValue ? to.Value.ToString("yyyy-MM-dd") : "";
                if (!IsOwner) ddlStatus.Items.Remove(ddlStatus.Items.FindByValue("Voided"));
            }

            HasFilter = q != "" || status != "" || from.HasValue || to.HasValue;

            var rows = Search(q, status, from, to);
            ResultCount = rows.Count;
            ResultSales = rows.Where(r => r.Status != "Voided").Sum(r => r.Total);

            PageCount = Math.Max(1, (int)Math.Ceiling(rows.Count / (double)PageSize));
            int page;
            PageNumber = int.TryParse(Request.QueryString["page"], out page) ? Math.Min(Math.Max(page, 1), PageCount) : 1;

            rptJobs.DataSource = rows.Skip((PageNumber - 1) * PageSize).Take(PageSize);
            rptJobs.DataBind();
            pnlEmpty.Visible = rows.Count == 0;
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            Response.Redirect(Link(txtSearch.Text.Trim(), ddlStatus.SelectedValue, txtFrom.Text, txtTo.Text, 1), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private List<HistoryRow> Search(string q, string status, DateTime? from, DateTime? to)
        {
            var customers = SampleData.Customers(Session).ToDictionary(c => c.Id);
            string term = q.ToLowerInvariant();
            string digits = SampleData.Digits(q);

            var rows = SampleData.Jobs()
                .Where(j => IsOwner || j.Status != "Voided")
                .Select(j => new HistoryRow(j, customers[j.CustomerId]));

            // One box finds a claim number, a name, or a contact number typed with or without spaces.
            if (term != "")
                rows = rows.Where(r => r.Claim.ToLowerInvariant().Contains(term)
                                    || r.Customer.ToLowerInvariant().Contains(term)
                                    || (digits.Length >= 4 && SampleData.Digits(r.Contact).Contains(digits)));

            if (status == "Active")
                rows = rows.Where(r => r.Status != "Claimed" && r.Status != "Voided");
            else if (status == "Claimed" || status == "Voided")
                rows = rows.Where(r => r.Status == status);

            // The dates filter on drop-off day, and "to" includes the whole of that day.
            if (from.HasValue) rows = rows.Where(r => r.DroppedOff >= from.Value);
            if (to.HasValue) rows = rows.Where(r => r.DroppedOff < to.Value.AddDays(1));

            return rows.OrderByDescending(r => r.DroppedOff).ToList();
        }

        private static DateTime? ParseDate(string value)
        {
            DateTime d;
            return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)
                ? d : (DateTime?)null;
        }

        // ---- Used by the markup ----

        /// <summary>The current filters with a different page number, for the pager links.</summary>
        protected string PageLink(int page)
        {
            return Link(Request.QueryString["q"], Request.QueryString["status"], Request.QueryString["from"], Request.QueryString["to"], page);
        }

        private static string Link(string q, string status, string from, string to, int page)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(q)) parts.Add("q=" + HttpUtility.UrlEncode(q));
            if (!string.IsNullOrEmpty(status)) parts.Add("status=" + HttpUtility.UrlEncode(status));
            if (!string.IsNullOrEmpty(from)) parts.Add("from=" + HttpUtility.UrlEncode(from));
            if (!string.IsNullOrEmpty(to)) parts.Add("to=" + HttpUtility.UrlEncode(to));
            if (page > 1) parts.Add("page=" + page);
            return "History.aspx" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
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
            return (at.Date == SampleData.Now.Date ? "Today" : at.ToString("MMM d")) + ", " + at.ToString("h:mm tt");
        }

        protected static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2");
        }

        public class HistoryRow
        {
            public string Claim { get; private set; }
            public string Customer { get; private set; }
            public string Contact { get; private set; }
            public string Service { get; private set; }
            public decimal WeightKg { get; private set; }
            public decimal Total { get; private set; }
            public string Method { get; private set; }
            public DateTime DroppedOff { get; private set; }
            public string CreatedBy { get; private set; }
            public string Status { get; private set; }
            public DateTime? ClosedAt { get; private set; }
            public string ClosedBy { get; private set; }
            public string VoidReason { get; private set; }

            public HistoryRow(SampleJob job, SampleCustomer customer)
            {
                Claim = job.Claim; Service = job.Service; WeightKg = job.WeightKg; Total = job.Total;
                Method = job.Method; DroppedOff = job.DroppedOff; CreatedBy = job.CreatedBy; Status = job.Status;
                ClosedAt = job.ClosedAt; ClosedBy = job.ClosedBy; VoidReason = job.VoidReason;
                Customer = customer.Name; Contact = customer.Contact;
            }

            /// <summary>The second line under the status: when a job was closed, or that it is still in the shop.</summary>
            public string StatusNote
            {
                get
                {
                    if (Status == "Claimed") return "Claimed " + When(ClosedAt.Value);
                    if (Status == "Voided") return "Voided " + When(ClosedAt.Value);
                    return "In the shop";
                }
            }
        }
    }
}

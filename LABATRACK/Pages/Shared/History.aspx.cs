using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using LABATRACK.DataAccess;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Pages.Shared
{
    // Past and current jobs, searched in SQL (usp_JobOrders_Search) one page at a time.
    //
    // The filters live in the query string, not in ViewState: Search redirects to
    // History.aspx?q=...&status=..., so a search can be bookmarked, the Back button works,
    // and refreshing never asks to resend a form.
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

            // Staff may search past jobs, but voided jobs, who did what, and sales totals are the
            // owner's (CLAUDE.md, Roles). Staff never see voided jobs, even by typing status=Voided.
            if (!IsOwner && status == "Voided") status = "";
            if (status != "Active" && status != "Claimed" && status != "Voided") status = "";

            if (!IsPostBack)
            {
                txtSearch.Text = q;
                if (!IsOwner) ddlStatus.Items.Remove(ddlStatus.Items.FindByValue("Voided"));
                ddlStatus.SelectedValue = ddlStatus.Items.FindByValue(status) != null ? status : "";
                txtFrom.Text = from.HasValue ? from.Value.ToString("yyyy-MM-dd", Inv) : "";
                txtTo.Text = to.HasValue ? to.Value.ToString("yyyy-MM-dd", Inv) : "";
            }

            HasFilter = q != "" || status != "" || from.HasValue || to.HasValue;

            // One box finds a claim number, a name, or a contact number typed with or without spaces.
            string text = q == "" ? null : q;
            string digits = CustomerService.Digits(q);
            if (digits.Length < 4) digits = null;
            string statusFilter = status == "" ? null : status;
            // The dates filter on drop-off day, and "to" includes the whole of that day.
            DateTime? toExclusive = to.HasValue ? to.Value.AddDays(1) : (DateTime?)null;

            int count;
            decimal sales;
            JobOrderRepository.SearchSummary(text, digits, statusFilter, from, toExclusive, IsOwner, out count, out sales);
            ResultCount = count;
            ResultSales = sales;

            PageCount = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
            int page;
            PageNumber = int.TryParse(Request.QueryString["page"], out page) ? Math.Min(Math.Max(page, 1), PageCount) : 1;

            rptJobs.DataSource = JobOrderRepository.Search(text, digits, statusFilter, from, toExclusive, IsOwner,
                                                          (PageNumber - 1) * PageSize, PageSize);
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

        /// <summary>The second line under the status: when a job was closed, or that it is still in the shop.</summary>
        protected static string StatusNote(JobOrder job)
        {
            if (job.CurrentStatus == StatusService.Claimed) return "Claimed " + When(job.StatusSince);
            if (job.CurrentStatus == StatusService.Voided) return "Voided " + When(job.StatusSince);
            return "In the shop";
        }

        /// <summary>Owner only: who handed the load over or voided it, and why it was voided.</summary>
        protected static string ClosedByNote(JobOrder job)
        {
            if (StatusService.IsInShop(job.CurrentStatus)) return "";
            bool voided = job.CurrentStatus == StatusService.Voided;
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

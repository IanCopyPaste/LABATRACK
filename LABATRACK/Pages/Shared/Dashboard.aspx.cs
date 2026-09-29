using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Pages.Shared
{
    // The job board (staff and owner): one column per stage, a card per load, and the one step
    // each card can take on its own. Everything comes from ReportService.GetDashboard, the same
    // method the owner's dashboard uses, so the two always agree.
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

        protected ReportService.DashboardData Data { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Data = ReportService.GetDashboard(DateTime.Now);
            if (!IsPostBack)
                Bind();
        }

        protected void Cards_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            // The argument carries the stage the card showed. StatusService moves the job only if it
            // is still in that stage (UPDATE ... WHERE CurrentStatus = @expected), which is what stops
            // two staff moving the same load twice: the second one is told to refresh.
            string[] parts = ((string)e.CommandArgument).Split('|');
            int jobId;
            if (e.CommandName != "Move" || parts.Length != 2 || !int.TryParse(parts[0], out jobId)) return;

            JobOrder job = Data.Active.FirstOrDefault(j => j.JobId == jobId);
            try
            {
                string to = StatusService.Advance(jobId, parts[1], CurrentUser.UserId);
                string claim = job != null ? job.ClaimNumber : "The job";
                ShowNotice("info", to == StatusService.Claimed ? claim + " was handed over (claimed)." : claim + " moved to " + to + ".");
            }
            catch (BusinessRuleException ex)
            {
                ShowNotice("warn", ex.Message);
            }

            Data = ReportService.GetDashboard(DateTime.Now);
            Bind();
        }

        private void Bind()
        {
            // Oldest drop-off first in every column (the query's order). There is no priority
            // ordering: every load runs on the same turnaround (see CLAUDE.md, Pricing rules).
            rptColumns.DataSource = StatusService.BoardStages.Select(stage => new BoardColumn
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
            string icon = kind == "warn" ? "circle-alert" : "circle-check";
            litNotice.Text = "<div class=\"notice notice-" + kind + "\">" + Icons.Get(icon, "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        // ---- Used by the markup ----

        /// <summary>The button names the step the card will take, which depends on the job's route.</summary>
        protected static string ActionLabel(JobOrder job)
        {
            string next = StatusService.NextStage(job);
            switch (next)
            {
                case StatusService.Washing: return "Start washing";
                case StatusService.Drying: return job.CurrentStatus == StatusService.Queued ? "Start drying" : "Move to drying";
                case StatusService.Folding: return job.CurrentStatus == StatusService.Queued ? "Start folding" : "Move to folding";
                case StatusService.Inspection: return "Send to inspection";
                case StatusService.Ready: return "Pass";
                default: return "Mark claimed";
            }
        }

        /// <summary>
        /// A load that is ready but still owes money: its card links to the job page, where the
        /// balance is taken and the load claimed in one save (StatusService.Claim).
        /// </summary>
        protected static bool CollectFirst(JobOrder job)
        {
            return job.CurrentStatus == StatusService.Ready && job.Balance > 0;
        }

        /// <summary>On a card whose service skips stages, says which, so nobody looks for it in Folding.</summary>
        protected static string RouteNote(JobOrder job)
        {
            var skipped = new List<string>();
            if (!job.IncludesWashing) skipped.Add("washing");
            if (!job.IncludesDrying) skipped.Add("drying");
            if (!job.IncludesFolding) skipped.Add("folding");
            return skipped.Count == 0 ? "" : "No " + string.Join(" or ", skipped);
        }

        /// <summary>Why a load came back from inspection, while it is being redone; empty otherwise.</summary>
        protected static string ReworkNote(JobOrder job)
        {
            bool sentBack = job.StatusFrom == StatusService.Inspection && StatusService.WorkStages(job).Contains(job.CurrentStatus);
            return sentBack && !string.IsNullOrEmpty(job.StatusReason) ? "Back from inspection: " + job.StatusReason : "";
        }

        /// <summary>
        /// What the filter box matches against: claim number, name, and contact number. The number
        /// is in twice, with and without spaces, so "0917555" and "0917 555" both find it.
        /// </summary>
        protected static string SearchText(JobOrder job)
        {
            string contact = job.ContactNumber ?? "";
            return (job.ClaimNumber + " " + job.CustomerName + " " + contact + " " + contact.Replace(" ", "")).ToLowerInvariant();
        }

        protected string UnclaimedNote(JobOrder job)
        {
            int days = ReportService.DaysWaiting(job.StatusSince, Data.Now);
            return "Ready since " + job.StatusSince.ToString("MMM d", Inv) + " · " + days + (days == 1 ? " day" : " days") + " unclaimed · "
                 + (job.Balance > 0 ? Peso(job.Balance) + " still to pay" : "paid");
        }

        protected static string VoidNote(JobOrder job)
        {
            return RefundNotes.ForVoid(job);
        }

        /// <summary>A refund from an earlier day that the customer has not collected yet.</summary>
        protected string RefundNote(JobOrder job)
        {
            return RefundNotes.Waiting(job, Data.Now);
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

        // Every card carries a day as well as a time, so a load left over from yesterday
        // cannot pass for one that came in this morning.
        protected string When(DateTime at)
        {
            return (at.Date == Data.Now.Date ? "Today" : at.ToString("MMM d", Inv)) + ", " + at.ToString("h:mm tt", Inv);
        }

        // How long since drop-off, kept short enough to sit beside the claim number.
        // Minutes stop mattering once a load has been in for ten hours.
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

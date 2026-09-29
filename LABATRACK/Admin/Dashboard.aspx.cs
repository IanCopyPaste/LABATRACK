using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Admin
{
    // The owner's dashboard: today's sales with the cash and e-wallet split, the loads per stage,
    // every active job, and what needs attention. It reads ReportService.GetDashboard, the same
    // method as the staff job board, so the two pages can never disagree.
    public partial class Dashboard : AdminBasePage
    {
        protected override string ActiveNav
        {
            get { return "Dashboard"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected ReportService.DashboardData Data { get; private set; }

        protected string Greeting
        {
            get
            {
                int hour = Data.Now.Hour;
                string part = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
                return part + ", " + CurrentUser.Username;
            }
        }

        // The split bar: each method's share of today's net sales, in percent.
        protected string CashShare { get { return Share(Data.Today.NetCash); } }
        protected string EwalletShare { get { return Share(Data.Today.NetEwallet); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            Data = ReportService.GetDashboard(DateTime.Now);

            // The owner's list shows the newest drop-off first, so a job just taken in is at the top.
            // (The job board keeps the oldest first in each column: that is the order loads are worked.)
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

        private string Share(decimal part)
        {
            decimal whole = Math.Max(0, Data.Today.NetCash) + Math.Max(0, Data.Today.NetEwallet);
            decimal pct = whole <= 0 ? 0 : Math.Max(0, part) / whole * 100;
            return pct.ToString("0.#", Inv);
        }

        // ---- Used by the markup ----

        /// <summary>The stage filter's choices, each with how many loads are at that stage now.</summary>
        protected string StageOptions()
        {
            var sb = new StringBuilder();
            sb.Append("<option value=\"\">All stages (").Append(Data.Active.Count).Append(")</option>");
            foreach (string stage in StatusService.BoardStages)
            {
                string encoded = HttpUtility.HtmlAttributeEncode(stage);
                sb.Append("<option value=\"").Append(encoded).Append("\">").Append(HttpUtility.HtmlEncode(stage))
                  .Append(" (").Append(Data.CountAt(stage)).Append(")</option>");
            }
            return sb.ToString();
        }

        /// <summary>What customers still owe on the loads in the shop (collected before each is claimed).</summary>
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

        protected static string PaymentBadgeFor(JobOrder job)
        {
            return Helpers.PaymentBadge.Html(job);
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

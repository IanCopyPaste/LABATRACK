using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using LABATRACK.DataAccess;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Pages.Shared
{
    // One job (?id=<JobId>): its customer and load, the stage stepper and the history it is read
    // from, the payments, the emails, and the one action for the stage it is in now. Every action
    // goes through StatusService (or EmailService for a resend), then comes back here with
    // ?done=... (post, then redirect), so a refresh never repeats it.
    public partial class JobDetail : BasePage
    {
        protected override string PageStyleSheet
        {
            get { return "JobDetail.css"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected JobOrder Job { get; private set; }
        protected bool IsVoided { get; private set; }
        protected bool InShop { get; private set; }
        protected string NextStage { get; private set; }
        protected bool MinimumApplied { get; private set; }
        protected bool ReadyReached { get; private set; }
        protected bool NothingPaid { get; private set; }
        protected string RefundNote { get; private set; }
        protected string BackUrl { get; private set; }
        protected string BackText { get; private set; }

        // The questions Confirm.js asks before a button moves the job (Helpers/MoveConfirm.cs) or voids it.
        protected string MoveTitle { get { return NextStage == null ? "" : MoveConfirm.Title(Job); } }
        protected string MoveText { get { return NextStage == null ? "" : MoveConfirm.Text(Job); } }
        protected string VoidText
        {
            get
            {
                return Job.CustomerName + " · " + Job.ServiceName + "\n\n"
                     + (Job.AmountPaid > 0
                        ? "The job is cancelled and the " + Peso(Job.AmountPaid) + " paid is refunded, by the same method it was paid."
                        : "The job is cancelled. Nothing was paid, so nothing is refunded.")
                     + " A voided job can't be undone; it stays in history with the reason.";
            }
        }

        // ---- Payment ----

        /// <summary>Something is still owed on a job that is still in the shop.</summary>
        protected bool HasBalance { get { return InShop && Job.Balance > 0; } }

        /// <summary>
        /// The "take a payment" form: while something is owed, before the load is ready. At Ready
        /// for pick-up the release card takes the whole balance instead, together with the claim.
        /// </summary>
        protected bool CanCollect { get { return HasBalance && Job.CurrentStatus != StatusService.Ready; } }

        protected string PaymentPill
        {
            get
            {
                if (IsVoided) return "pill-danger";
                if (Job.PaymentStatus == PaymentService.Paid) return "pill-ok";
                return Job.PaymentStatus == PaymentService.PartiallyPaid ? "pill-warn" : "pill-danger";
            }
        }

        protected string PaymentPillText
        {
            get
            {
                if (IsVoided) return "Refunded";
                return Job.PaymentStatus == PaymentService.Paid ? "Paid in full" : Job.PaymentStatus;
            }
        }

        private int jobId;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!int.TryParse(Request.QueryString["id"], out jobId) || JobOrderRepository.GetById(jobId) == null)
            {
                Response.Redirect("History.aspx", true);
                return;
            }

            if (IsPostBack)
                HandleAction();
            else
                ShowDoneNotice();

            // Read afresh after any action, so the page shows what is saved now.
            Bind();
        }

        // ------------------------------------------------------------ Actions

        // The buttons are plain submit buttons named action. The status the page showed travels in
        // hfShownStatus, and StatusService refuses the move if the job is no longer in it.
        private void HandleAction()
        {
            string shown = hfShownStatus.Value;
            int userId = CurrentUser.UserId;
            try
            {
                switch (Request.Form["action"])
                {
                    case "advance":
                        StatusService.Advance(jobId, shown, userId);
                        Done("moved");
                        break;
                    case "rework":
                        StatusService.SendBack(jobId, ddlReworkStage.SelectedValue, txtReworkReason.Text, userId);
                        Done("rework");
                        break;
                    case "void":
                        StatusService.Void(jobId, txtVoidReason.Text, userId);
                        Done("voided");
                        break;
                    case "pay":
                        PaymentService.Collect(jobId, PostedPesos("payAmount"), Request.Form["payMethod"],
                                               PostedCash("payMethod", "payCash"), ShownBalance(), userId);
                        Done("paid");
                        break;
                    case "claim":
                        // Takes the whole balance and hands the load over, in one save.
                        StatusService.Claim(jobId, ShownBalance(), Request.Form["claimMethod"],
                                            PostedCash("claimMethod", "claimCashReceived"), userId);
                        Done("moved");
                        break;
                    case "resend-dropoff":
                        CheckStillInShop();
                        EmailService.SendDropOff(jobId, userId, true);
                        Done("resent");
                        break;
                    case "resend-ready":
                        CheckStillInShop();
                        EmailService.SendReady(jobId, userId, true);
                        Done("resent");
                        break;
                    case "resend-voided":
                        if (JobOrderRepository.GetById(jobId).CurrentStatus != StatusService.Voided)
                            throw new BusinessRuleException("Only a voided job has a cancellation email.");
                        EmailService.SendVoided(jobId, userId, true);
                        Done("resent");
                        break;
                }
            }
            catch (BusinessRuleException ex)
            {
                ShowNotice("warn", ex.Message);
            }
        }

        /// <summary>The balance the page showed (hfShownBalance), or -1 if it is missing, which no balance matches.</summary>
        private decimal ShownBalance()
        {
            decimal shown;
            return decimal.TryParse(hfShownBalance.Value, NumberStyles.Number, Inv, out shown) ? shown : -1m;
        }

        /// <summary>An amount typed in a plain form box: "1,500", "₱150" and "150.00" all read as numbers.</summary>
        private decimal PostedPesos(string name)
        {
            decimal amount;
            if (!TryPesos(Request.Form[name], out amount))
                throw new BusinessRuleException("Enter the amount paid in pesos, for example 100 or 100.00.");
            return amount;
        }

        /// <summary>The cash handed over, read only when Cash was chosen. Unreadable counts as none, which PaymentService refuses.</summary>
        private decimal? PostedCash(string methodName, string cashName)
        {
            decimal cash;
            if (Request.Form[methodName] != Payment.Cash) return null;
            return TryPesos(Request.Form[cashName], out cash) ? cash : (decimal?)null;
        }

        private static bool TryPesos(string text, out decimal amount)
        {
            text = (text ?? "").Replace(",", "").Replace("₱", "").Trim();
            return decimal.TryParse(text, NumberStyles.Number, Inv, out amount);
        }

        // Emails are resent only while the load is still in the shop; after that there is nothing to tell.
        private void CheckStillInShop()
        {
            if (!StatusService.IsInShop(JobOrderRepository.GetById(jobId).CurrentStatus))
                throw new BusinessRuleException("This job is closed, so its emails are not sent again.");
        }

        private void Done(string what)
        {
            Response.Redirect("JobDetail.aspx?id=" + jobId + "&done=" + what, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ------------------------------------------------------------ Showing the job

        private void Bind()
        {
            Job = JobOrderRepository.GetById(jobId);
            IsVoided = Job.CurrentStatus == StatusService.Voided;
            InShop = StatusService.IsInShop(Job.CurrentStatus);
            NextStage = StatusService.NextStage(Job);
            MinimumApplied = PricingService.Quote(Job.WeightKg, Job.RatePerKgAtCreation, Job.MinimumChargeAtCreation,
                                                  Job.RoundingIncrementKgAtCreation, null).MinimumApplied;
            hfShownStatus.Value = Job.CurrentStatus;
            hfShownBalance.Value = Money(Job.Balance);
            Title = "Job " + Job.ClaimNumber + " · LabaTrack";
            BackUrl = ResolveUrl(IsOwner ? "~/Admin/Dashboard.aspx" : "~/Pages/Shared/Dashboard.aspx");
            BackText = IsOwner ? "Back to dashboard" : "Back to job board";

            List<StatusChange> history = StatusHistoryRepository.GetByJob(jobId);
            ReadyReached = history.Any(h => h.ToStatus == StatusService.Ready);
            rptStepper.DataSource = BuildSteps(history);
            rptStepper.DataBind();
            rptHistory.DataSource = history.AsEnumerable().Reverse();   // newest first
            rptHistory.DataBind();

            rptLines.DataSource = JobOrderRepository.GetAddOns(jobId);
            rptLines.DataBind();

            List<Payment> payments = PaymentRepository.GetByJob(jobId);
            rptPayments.DataSource = payments;
            rptPayments.DataBind();
            NothingPaid = payments.Count == 0;
            RefundNote = BuildRefundNote(payments);

            rptEmails.DataSource = NotificationRepository.GetByJob(jobId);
            rptEmails.DataBind();

            // Only the current stage gets an action.
            string status = Job.CurrentStatus;
            pnlQueued.Visible = status == StatusService.Queued;
            pnlAdvance.Visible = StatusService.WorkStages(Job).Contains(status);
            pnlInspection.Visible = status == StatusService.Inspection;
            pnlReady.Visible = status == StatusService.Ready;
            pnlClosed.Visible = !InShop;

            // A failed inspection can only go back to a work stage this job actually has.
            if (pnlInspection.Visible && ddlReworkStage.Items.Count == 0)
            {
                foreach (string stage in StatusService.ReworkChoices(Job, SettingsRepository.Get()))
                    ddlReworkStage.Items.Add(stage);
                ddlReworkStage.SelectedIndex = ddlReworkStage.Items.Count - 1;
            }
        }

        /// <summary>
        /// The stepper: only the stages on this job's route. A completed stage shows when it was
        /// last entered and by whom (a stage done twice after a rework shows the later time), the
        /// current one is highlighted, future ones are grey. Voided is shown as its own red banner,
        /// not as a step.
        /// </summary>
        private List<StepItem> BuildSteps(List<StatusChange> history)
        {
            List<string> route = StatusService.Route(Job);
            int current = route.IndexOf(Job.CurrentStatus);
            bool claimed = Job.CurrentStatus == StatusService.Claimed;

            var steps = new List<StepItem>();
            for (int i = 0; i < route.Count; i++)
            {
                var step = new StepItem { Name = route[i], Number = i + 1 };
                if (IsVoided)
                    step.State = i == 0 ? "done" : "future";
                else if (i < current || claimed)
                    step.State = "done";
                else if (i == current)
                    step.State = "current";
                else
                    step.State = "future";

                StatusChange entered = history.LastOrDefault(h => h.ToStatus == route[i]);
                if (entered != null && step.State != "future")
                {
                    step.When = When(entered.ChangedAt);
                    step.By = entered.ChangedBy;
                }
                steps.Add(step);
            }
            return steps;
        }

        private static string BuildRefundNote(List<Payment> payments)
        {
            var parts = payments.Where(p => p.Kind == Payment.VoidRefund)
                                .Select(p => Peso(-p.Amount) + (p.Method == Payment.Cash ? " in cash" : " by e-wallet"))
                                .ToList();
            return parts.Count == 0 ? "Nothing had been paid, so nothing was refunded." : "Refunded " + string.Join(" and ", parts) + ".";
        }

        private void ShowDoneNotice()
        {
            JobOrder job = JobOrderRepository.GetById(jobId);
            string text;
            switch (Request.QueryString["done"])
            {
                case "moved":
                    if (job.CurrentStatus == StatusService.Ready)
                        text = "Passed inspection: ready for pick-up. The ready email is listed under Notifications.";
                    else if (job.CurrentStatus == StatusService.Claimed)
                        text = "Marked as claimed: paid in full and handed over.";
                    else
                        text = "Moved to " + job.CurrentStatus + ".";
                    break;
                case "paid":
                    text = job.Balance > 0
                        ? "Payment recorded. " + Peso(job.Balance) + " is still owed, collected when the load is claimed."
                        : "Payment recorded. The job is now paid in full.";
                    break;
                case "rework": text = "Sent back to " + job.CurrentStatus + " to be redone."; break;
                case "voided": text = "Voided. " + BuildRefundNote(PaymentRepository.GetByJob(jobId)) + " The cancellation email is listed under Notifications."; break;
                case "resent": text = "Email tried again. The result is listed under Notifications."; break;
                case "edited":
                    text = "Correction saved. " + (job.Balance > 0
                        ? "The balance is now " + Peso(job.Balance) + ", collected before pick-up."
                        : "Nothing is owed on this job. A refund, if one was due, is listed under Payment.")
                        + " The customer's update email is listed under Notifications.";
                    break;
                default: return;
            }
            ShowNotice("info", text);
        }

        private void ShowNotice(string kind, string text)
        {
            string icon = kind == "warn" ? "circle-alert" : "circle-check";
            litNotice.Text = "<div class=\"notice notice-" + kind + "\">" + Icons.Get(icon, "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        // ---- Used by the markup ----

        protected static string Peso(decimal amount)
        {
            return (amount < 0 ? "−₱" : "₱") + Math.Abs(amount).ToString("N2", Inv);
        }

        protected static string Kg(decimal kg)
        {
            return kg.ToString("0.0#", Inv);
        }

        protected static string When(DateTime at)
        {
            return at.ToString("MMM d, h:mm tt", Inv);
        }

        protected static decimal LineAmount(JobAddOn line)
        {
            return PricingService.LineAmount(line.Quantity, line.PriceAtCreation);
        }

        protected static string PaymentLabel(Payment p)
        {
            if (p.Kind == Payment.DropOff) return "Paid at drop-off · " + p.Method;
            if (p.Kind == Payment.BalancePayment) return "Balance payment · " + p.Method;
            if (p.Kind == Payment.VoidRefund) return "Refund on void · " + p.Method;
            return (p.Amount > 0 ? "Collected on correction · " : "Refunded on correction · ") + p.Method;
        }

        /// <summary>" · cash ₱500.00, change ₱210.00" for a cash payment; empty otherwise.</summary>
        protected static string CashNote(Payment p)
        {
            return p.CashReceived.HasValue ? " · cash " + Peso(p.CashReceived.Value) + ", change " + Peso(p.Change) : "";
        }

        /// <summary>A value typed in a plain form box, kept after a refused post; else the fallback.</summary>
        protected string Posted(string name, string fallback)
        {
            return IsPostBack && Request.Form[name] != null ? Request.Form[name] : fallback;
        }

        /// <summary>True if the refused post chose this value (for keeping a radio button's choice).</summary>
        protected bool IsPosted(string name, string value)
        {
            return IsPostBack && Request.Form[name] == value;
        }

        /// <summary>Money for a data- attribute or hidden field: dot decimal point, no grouping.</summary>
        protected static string Money(decimal amount)
        {
            return amount.ToString("0.00", Inv);
        }

        protected static string EmailDetail(NotificationLogEntry n)
        {
            string who = n.IsResend ? " · resent by " + n.AttemptedBy : "";
            if (n.Status == NotificationLogEntry.Sent) return "to " + n.Recipient + who;
            return (n.Detail ?? n.Status) + who;
        }

        protected static string EmailPill(string status)
        {
            if (status == NotificationLogEntry.Sent) return "pill-ok";
            if (status == NotificationLogEntry.Failed) return "pill-danger";
            return "st-claimed";
        }

        protected static string EmailDotStyle(string status)
        {
            return status == NotificationLogEntry.Failed
                ? "background:var(--danger-soft); color:var(--danger);"
                : "background:var(--primary-soft); color:var(--primary);";
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

        public class StepItem
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public string State { get; set; }
            public string When { get; set; }
            public string By { get; set; }
        }
    }
}

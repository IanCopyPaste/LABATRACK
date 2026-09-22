using System;
using System.Collections.Generic;
using System.Linq;
using LABATRACK.Security;

namespace LABATRACK.Pages.Shared
{
    // DRAFT: design only, built from sample data so every stage can be previewed
    // with ?stage=Queued, Washing, Inspection, Ready, Claimed or Voided.
    // Phase 4 replaces the sample lists with JobStatusHistory and Payments rows,
    // and the buttons call StatusService.
    public partial class JobDetail : BasePage
    {
        protected override string PageStyleSheet
        {
            get { return "JobDetail.css"; }
        }

        private static readonly string[] Stages =
            { "Queued", "Washing", "Drying", "Folding", "Inspection", "Ready for pick-up", "Claimed" };

        // Sample JobStatusHistory rows for LT-260921-004, including one failed inspection.
        private static readonly List<HistoryRow> SampleHistory = new List<HistoryRow>
        {
            new HistoryRow("",                  "Queued",            "Sep 21, 9:12 AM",  "jcruz",   ""),
            new HistoryRow("Queued",            "Washing",           "Sep 21, 9:40 AM",  "jcruz",   ""),
            new HistoryRow("Washing",           "Drying",            "Sep 21, 10:35 AM", "jcruz",   ""),
            new HistoryRow("Drying",            "Folding",           "Sep 21, 11:50 AM", "arivera", ""),
            new HistoryRow("Folding",           "Inspection",        "Sep 21, 12:30 PM", "arivera", ""),
            new HistoryRow("Inspection",        "Folding",           "Sep 21, 12:45 PM", "arivera", "Shirts creased, refold neatly"),
            new HistoryRow("Folding",           "Inspection",        "Sep 21, 1:20 PM",  "arivera", ""),
            new HistoryRow("Inspection",        "Ready for pick-up", "Sep 21, 1:35 PM",  "arivera", ""),
            new HistoryRow("Ready for pick-up", "Claimed",           "Sep 22, 4:10 PM",  "jcruz",   ""),
        };

        protected string CurrentStage { get; private set; }
        protected string NextStage { get; private set; }
        protected bool IsVoided { get; private set; }
        protected decimal Total = 305.00m;
        protected decimal CashReceived = 500.00m;
        protected decimal Change { get { return CashReceived - Total; } }   // change = cash received - total
        protected bool ReadyEmailSent { get { return CurrentStage == "Ready for pick-up" || CurrentStage == "Claimed"; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            CurrentStage = PickPreviewStage(Request.QueryString["stage"]);
            IsVoided = CurrentStage == "Voided";

            var history = BuildHistory();
            var payments = BuildPayments();

            int current = Array.IndexOf(Stages, CurrentStage);
            NextStage = current >= 0 && current < Stages.Length - 1 ? Stages[current + 1] : "";

            rptStepper.DataSource = BuildSteps(history, current);
            rptStepper.DataBind();
            rptHistory.DataSource = history.AsEnumerable().Reverse();   // newest first
            rptHistory.DataBind();
            rptPayments.DataSource = payments;
            rptPayments.DataBind();

            // Only the current stage gets an action.
            pnlQueued.Visible = CurrentStage == "Queued";
            pnlAdvance.Visible = CurrentStage == "Washing" || CurrentStage == "Drying" || CurrentStage == "Folding";
            pnlInspection.Visible = CurrentStage == "Inspection";
            pnlReady.Visible = CurrentStage == "Ready for pick-up";
            pnlClosed.Visible = CurrentStage == "Claimed" || IsVoided;
        }

        private static string PickPreviewStage(string requested)
        {
            switch (requested)
            {
                case "Queued": return "Queued";
                case "Washing": return "Washing";
                case "Inspection": return "Inspection";
                case "Claimed": return "Claimed";
                case "Voided": return "Voided";
                default: return "Ready for pick-up";
            }
        }

        private List<HistoryRow> BuildHistory()
        {
            if (IsVoided)
                return new List<HistoryRow>
                {
                    SampleHistory[0],
                    new HistoryRow("Queued", "Voided", "Sep 21, 9:20 AM", "jcruz", "Customer changed mind, took the load home"),
                };

            // Everything up to the latest time the job entered the preview stage.
            int last = SampleHistory.FindLastIndex(h => h.To == CurrentStage);
            return SampleHistory.Take(last + 1).ToList();
        }

        private List<PaymentRow> BuildPayments()
        {
            var list = new List<PaymentRow> { new PaymentRow("Sep 21, 9:12 AM", "Cash", 305.00m, "jcruz", false) };
            if (IsVoided)
                list.Add(new PaymentRow("Sep 21, 9:20 AM", "Cash", -305.00m, "jcruz", true));
            return list;
        }

        private List<StepItem> BuildSteps(List<HistoryRow> history, int current)
        {
            var steps = new List<StepItem>();
            for (int i = 0; i < Stages.Length; i++)
            {
                var step = new StepItem { Name = Stages[i], Number = i + 1 };
                var entered = history.LastOrDefault(h => h.To == Stages[i]);

                if (IsVoided)
                    step.State = i == 0 ? "done" : "future";
                else if (i < current || CurrentStage == "Claimed")
                    step.State = "done";
                else if (i == current)
                    step.State = "current";
                else
                    step.State = "future";

                if (entered != null && step.State != "future")
                {
                    step.When = entered.At;
                    step.By = entered.By;
                }
                steps.Add(step);
            }
            return steps;
        }

        protected static string Peso(decimal amount)
        {
            return (amount < 0 ? "−₱" : "₱") + Math.Abs(amount).ToString("N2");
        }

        protected string StageCss(string stage)
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

        public class HistoryRow
        {
            public string From { get; set; }
            public string To { get; set; }
            public string At { get; set; }
            public string By { get; set; }
            public string Reason { get; set; }
            public HistoryRow(string from, string to, string at, string by, string reason)
            {
                From = from; To = to; At = at; By = by; Reason = reason;
            }
        }

        public class PaymentRow
        {
            public string At { get; set; }
            public string Method { get; set; }
            public decimal Amount { get; set; }
            public string By { get; set; }
            public bool IsRefund { get; set; }
            public PaymentRow(string at, string method, decimal amount, string by, bool isRefund)
            {
                At = at; Method = method; Amount = amount; By = by; IsRefund = isRefund;
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

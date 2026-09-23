using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.Helpers;
using LABATRACK.Security;

namespace LABATRACK.Pages.Shared
{
    // DRAFT: the job board, built from sample data so it can be clicked through.
    // The jobs live in ViewState, so moving a card lasts until the page is opened afresh.
    // Phase 4 replaces SampleJobs with the shared repository query (the same one the owner
    // dashboard uses) and Move() with StatusService, which writes JobStatusHistory.
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

        // The stages a load passes through while it is on the board. Claimed and Voided
        // are final, so those jobs leave the board.
        private static readonly string[] BoardStages =
            { "Queued", "Washing", "Drying", "Folding", "Inspection", "Ready for pick-up" };

        // The sample data is a snapshot of one morning, so ages and new timestamps are
        // measured from this moment rather than the real clock.
        private static readonly DateTime SampleNow = new DateTime(2026, 9, 22, 12, 0, 0);
        private const string SignedInUser = "owner";   // DRAFT: Phase 2 reads this from AuthService

        // DRAFT: today's payments net of refunds (6 payments, 1 refund). Claiming a load adds
        // nothing, because every job is paid in full at drop-off.
        protected const decimal TodaysTotal = 850.00m;

        private List<BoardJob> Jobs
        {
            get { return (List<BoardJob>)ViewState["Jobs"]; }
            set { ViewState["Jobs"] = value; }
        }

        private List<BoardJob> ClaimedToday
        {
            get { return (List<BoardJob>)ViewState["Claimed"]; }
            set { ViewState["Claimed"] = value; }
        }

        protected int ActiveCount { get { return Jobs.Count; } }
        protected int ReadyCount { get { return Jobs.Count(j => j.Stage == "Ready for pick-up"); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Jobs = SampleJobs();
                ClaimedToday = SampleClaimed();
                Bind();
            }
        }

        protected void Cards_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            // The argument carries the stage the card showed. Phase 4 passes it to StatusService
            // as the expected status (UPDATE ... WHERE CurrentStatus = @expected), which is what
            // stops two staff moving the same load twice. The draft keeps the same check, but
            // each window holds its own ViewState copy, so it cannot catch a second window yet.
            string[] parts = ((string)e.CommandArgument).Split('|');
            string claim = parts[0], shownStage = parts[1];

            BoardJob job = Jobs.FirstOrDefault(j => j.Claim == claim);
            if (job == null || job.Stage != shownStage)
            {
                ShowNotice("warn", "This job was already updated. Refresh.");
                Bind();
                return;
            }

            string to = NextStage(job.Stage);
            Move(job, to);
            ShowNotice("info", job.Claim + " moved to " + to + ".");
            Bind();
        }

        // Mirrors what StatusService will do: one forward step, stamped with the time and
        // the staff member. Claimed takes the job off the board.
        private void Move(BoardJob job, string to)
        {
            job.Stage = to;
            job.StageSince = SampleNow;
            job.StageBy = SignedInUser;
            job.Rework = "";

            if (to == "Claimed")
            {
                Jobs.Remove(job);
                ClaimedToday.Add(job);
            }
        }

        // Only the steps a card can take on its own. A failed inspection and a void both
        // need a reason, so those happen on JobDetail.
        private static string NextStage(string stage)
        {
            switch (stage)
            {
                case "Queued": return "Washing";
                case "Washing": return "Drying";
                case "Drying": return "Folding";
                case "Folding": return "Inspection";
                case "Inspection": return "Ready for pick-up";
                default: return "Claimed";
            }
        }

        private void Bind()
        {
            // Oldest drop-off first in every column. There is no priority ordering: every
            // load runs on the same turnaround (see CLAUDE.md, Pricing rules).
            rptColumns.DataSource = BoardStages.Select(stage => new BoardColumn
            {
                Stage = stage,
                Jobs = Jobs.Where(j => j.Stage == stage).OrderBy(j => j.DroppedOff).ToList(),
            });
            rptColumns.DataBind();

            rptClaimed.DataSource = ClaimedToday.OrderByDescending(j => j.StageSince);
            rptClaimed.DataBind();
        }

        private void ShowNotice(string kind, string text)
        {
            string icon = kind == "warn" ? "circle-alert" : "circle-check";
            litNotice.Text = "<div class=\"notice notice-" + kind + "\">" + Icons.Get(icon, "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        // ---- Used by the markup ----

        protected static string ActionLabel(string stage)
        {
            switch (stage)
            {
                case "Queued": return "Start washing";
                case "Washing": return "Move to drying";
                case "Drying": return "Move to folding";
                case "Folding": return "Send to inspection";
                case "Inspection": return "Pass";
                default: return "Mark claimed";
            }
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
        protected static string When(DateTime at)
        {
            return (at.Date == SampleNow.Date ? "Today" : at.ToString("MMM d")) + ", " + at.ToString("h:mm tt");
        }

        // How long since drop-off, kept short enough to sit beside the claim number.
        // Minutes stop mattering once a load has been in for ten hours.
        protected static string Age(DateTime droppedOff)
        {
            TimeSpan age = SampleNow - droppedOff;
            if (age.TotalHours < 1) return (int)age.TotalMinutes + "m";
            if (age.TotalHours < 10) return (int)age.TotalHours + "h " + age.Minutes + "m";
            if (age.TotalDays < 1) return (int)age.TotalHours + "h";
            return (int)age.TotalDays + "d " + age.Hours + "h";
        }

        protected static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2");
        }

        // ---- Sample data ----

        private static List<BoardJob> SampleJobs()
        {
            return new List<BoardJob>
            {
                // Two loads ready for over a week: still on the board, because a job leaves
                // only when it is claimed or voided. They are also under Needs attention.
                Job("LT-260912-003", "Rosa Lim", "0917 555 0110", "Wash-Dry-Fold", 4.0m, 160.00m, "",
                    At(12, 16, 20), "Ready for pick-up", At(13, 10, 40), "arivera", ""),
                Job("LT-260914-008", "Dennis Uy", "0918 555 0111", "Wash-Dry", 5.5m, 210.00m, "",
                    At(14, 10, 0), "Ready for pick-up", At(15, 9, 30), "jcruz", ""),
                Job("LT-260921-004", "Maria Santos", "0917 555 0123", "Wash-Dry-Fold", 5.1m, 290.00m, "Extra rinse, Fabric conditioner × 2",
                    At(21, 9, 12), "Ready for pick-up", At(21, 13, 35), "arivera", ""),
                Job("LT-260921-007", "Jose Dela Cruz", "0918 555 0177", "Wash-Dry-Fold", 3.2m, 160.00m, "",
                    At(21, 15, 40), "Inspection", At(22, 11, 25), "arivera", ""),
                Job("LT-260922-001", "Ana Reyes", "0917 555 0101", "Comforter / Bulky", 2.0m, 120.00m, "",
                    At(22, 8, 5), "Folding", At(22, 11, 40), "arivera", "Back from inspection: creases on the cover"),
                Job("LT-260922-002", "Carlo Mendoza", "0927 555 0102", "Wash-Dry-Fold", 2.3m, 135.00m, "Fabric conditioner × 1",
                    At(22, 8, 31), "Drying", At(22, 10, 50), "jcruz", ""),
                Job("LT-260922-003", "Liza Peñaflor", "0917 555 0142", "Wash-Dry", 4.6m, 195.00m, "Extra rinse",
                    At(22, 9, 2), "Washing", At(22, 10, 20), "jcruz", ""),
                Job("LT-260922-004", "Ramon Bautista", "0939 555 0104", "Wash-Dry-Fold", 7.0m, 280.00m, "",
                    At(22, 10, 15), "Queued", At(22, 10, 15), "jcruz", ""),
                Job("LT-260922-005", "Grace Villanueva", "0917 555 0105", "Wash-Dry-Fold", 1.5m, 120.00m, "",
                    At(22, 10, 48), "Queued", At(22, 10, 48), "jcruz", ""),
            };
        }

        private static List<BoardJob> SampleClaimed()
        {
            return new List<BoardJob>
            {
                Job("LT-260920-011", "Paolo Garcia", "0917 555 0108", "Wash-Dry-Fold", 6.0m, 240.00m, "",
                    At(20, 14, 10), "Claimed", At(22, 10, 5), "jcruz", ""),
                Job("LT-260921-002", "Nena Aquino", "0998 555 0109", "Wash-Dry-Fold", 3.0m, 150.00m, "Stain treatment",
                    At(21, 8, 40), "Claimed", At(22, 11, 42), "jcruz", ""),
            };
        }

        private static DateTime At(int day, int hour, int minute)
        {
            return new DateTime(2026, 9, day, hour, minute, 0);
        }

        private static BoardJob Job(string claim, string customer, string contact, string service, decimal kg, decimal total,
                                    string addOns, DateTime droppedOff, string stage, DateTime since, string by, string rework)
        {
            return new BoardJob
            {
                Claim = claim, Customer = customer, Contact = contact, Service = service, WeightKg = kg, Total = total,
                AddOns = addOns, DroppedOff = droppedOff, Stage = stage, StageSince = since, StageBy = by, Rework = rework,
            };
        }

        [Serializable]
        public class BoardJob
        {
            public string Claim { get; set; }
            public string Customer { get; set; }
            public string Contact { get; set; }
            public string Service { get; set; }
            public decimal WeightKg { get; set; }
            public decimal Total { get; set; }
            public string AddOns { get; set; }
            public DateTime DroppedOff { get; set; }
            public string Stage { get; set; }
            public DateTime StageSince { get; set; }
            public string StageBy { get; set; }
            public string Rework { get; set; }   // why the load came back, while it is being redone

            // What the filter box matches against: claim number, name, and contact number.
            public string SearchText
            {
                // The contact number is in twice, with and without spaces, so "0917555" and
                // "0917 555" both find it.
                get { return (Claim + " " + Customer + " " + Contact + " " + Contact.Replace(" ", "")).ToLowerInvariant(); }
            }
        }

        public class BoardColumn
        {
            public string Stage { get; set; }
            public List<BoardJob> Jobs { get; set; }
        }
    }
}

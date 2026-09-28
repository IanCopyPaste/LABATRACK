using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using LABATRACK.Security;

namespace LABATRACK.Admin
{
    // DRAFT: design only. The page shows sample data until its phase is built.
    public partial class Pricing : BasePage
    {
        protected override string ActiveNav
        {
            get { return "Pricing"; }
        }

        protected override string PageStyleSheet
        {
            get { return "Pricing.css"; }
        }

        // The three work stages a service can include, always in this order. Queued comes before
        // them and Inspection, Ready for pick-up and Claimed after, for every service.
        private static readonly string[] WorkStages = { "Washing", "Drying", "Folding" };

        protected void Page_Load(object sender, EventArgs e)
        {
            // DRAFT: sample services. Which stages each one has is the owner's choice; these are
            // only examples. LoadsInShop matches the nine loads on the sample job board. Phase 2
            // reads the Services table, and the count from jobs that are not Claimed or Voided.
            rptServices.DataSource = new List<ServiceRow>
            {
                new ServiceRow("Wash-Dry-Fold", 40m, true, true, true, true, 6),
                new ServiceRow("Wash-Dry", 35m, true, true, true, false, 2),
                new ServiceRow("Comforter / Bulky", 60m, true, true, true, true, 1),
                new ServiceRow("Wash Only", 25m, false, true, false, false, 0),
            };
            rptServices.DataBind();
        }

        /// <summary>All three stages as chips; the ones this service skips are faded and struck through.</summary>
        protected static string StageChips(string code)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < WorkStages.Length; i++)
            {
                bool on = code.IndexOf(WorkStages[i][0]) >= 0;
                sb.Append("<span class=\"stage-chip").Append(on ? "" : " is-off").Append("\"")
                  .Append(on ? "" : " title=\"Skipped by this service\"").Append(">")
                  .Append(HttpUtility.HtmlEncode(WorkStages[i])).Append("</span>");
            }
            return sb.ToString();
        }

        public class ServiceRow
        {
            public string Name { get; private set; }
            public decimal Rate { get; private set; }
            public bool Active { get; private set; }
            public string StageCode { get; private set; }   // "WDF", "WD", "W": the work stages it includes
            public int LoadsInShop { get; private set; }     // jobs with this service not yet Claimed or Voided

            public ServiceRow(string name, decimal rate, bool active, bool washing, bool drying, bool folding, int loadsInShop)
            {
                Name = name; Rate = rate; Active = active; LoadsInShop = loadsInShop;
                StageCode = (washing ? "W" : "") + (drying ? "D" : "") + (folding ? "F" : "");
            }

            // A service with a load still in the shop cannot be changed at all (CLAUDE.md, Pricing rules).
            public bool Locked { get { return LoadsInShop > 0; } }
        }
    }
}

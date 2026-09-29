using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.DataAccess;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Admin
{
    // The owner's correction of a job (?id=<JobId>) that is not yet Claimed or Voided: a wrong
    // weight, service, add-on, remark or customer detail. The new total is worked out with the
    // prices frozen on the job, and the difference is settled in the same save (collected or
    // refunded), so the job's payments always add up to its total. JobOrderService.SaveEdit
    // checks every rule; this page only collects what was typed and shows the result.
    public partial class EditJob : AdminBasePage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected JobOrder Job { get; private set; }
        protected decimal AmountPaid { get; private set; }
        protected PriceQuote Quote { get; private set; }
        protected decimal Difference { get; private set; }
        protected string SettleState { get; private set; }   // collect, refund or none
        protected string RatesData { get; private set; }
        protected string MaxWeightData { get; private set; }

        // The service can change only while the job is still Queued: a different service can
        // have a different route, and a load already in, say, Folding may not have that stage
        // on the new one. Weight, add-on and customer fixes stay allowed until it is claimed.
        protected bool ServiceChangeAllowed
        {
            get { return Job.CurrentStatus == StatusService.Queued; }
        }

        private List<JobAddOn> currentLines;
        private List<LaundryService> services;
        private List<AddOn> addOns;

        protected void Page_Load(object sender, EventArgs e)
        {
            int id;
            Job = int.TryParse(Request.QueryString["id"], out id) ? JobOrderRepository.GetById(id) : null;
            if (Job == null)
            {
                Response.Redirect("Dashboard.aspx", true);
                return;
            }
            if (!StatusService.IsInShop(Job.CurrentStatus))
            {
                // Claimed and Voided are final: there is nothing left to correct.
                Response.Redirect("../Pages/Shared/JobDetail.aspx?id=" + Job.JobId, true);
                return;
            }

            currentLines = JobOrderRepository.GetAddOns(Job.JobId);
            AmountPaid = PaymentRepository.GetJobTotals(Job.JobId).AmountPaid;
            ShopSettings settings = SettingsRepository.Get();
            MaxWeightData = settings.MaxWeightKg.ToString("0.##", Inv);

            // What can be chosen: every active service and add-on, plus whatever is already on the
            // job even if it is no longer offered (it keeps its frozen price).
            services = ServiceRepository.GetAll().Where(s => s.IsActive || s.ServiceId == Job.ServiceId).ToList();
            addOns = AddOnRepository.GetAll().Where(a => a.IsActive || currentLines.Any(l => l.AddOnId == a.AddOnId)).ToList();
            RatesData = string.Join(",", services.Select(s => s.ServiceId + ":" + Money(RateFor(s))));

            if (!IsPostBack)
            {
                txtCustomerName.Text = Job.CustomerName;
                txtContactNumber.Text = Job.ContactNumber;
                txtEmail.Text = Job.CustomerEmail;
                txtWeight.Text = Job.WeightKg.ToString("0.0#", Inv);
                txtRemarks.Text = Job.Remarks;
                foreach (LaundryService s in services)
                    ddlService.Items.Add(new ListItem(s.Name + " · ₱" + RateFor(s).ToString("N2", Inv) + "/kg", s.ServiceId.ToString(Inv)));
                ddlService.SelectedValue = Job.ServiceId.ToString(Inv);
                hfShownStatus.Value = Job.CurrentStatus;
                // The difference is settled by the same method the job was paid with, unless the owner picks the other.
                rbAdjCash.Checked = Job.PaymentMethod != Payment.Ewallet;
                rbAdjEwallet.Checked = Job.PaymentMethod == Payment.Ewallet;
            }
            ddlService.Enabled = ServiceChangeAllowed;

            if (IsPostBack && Request.Form["action"] == "save")
                Save();

            rptServiceAddOns.DataSource = addOns.Where(a => !a.IsProduct);
            rptServiceAddOns.DataBind();
            rptProducts.DataSource = addOns.Where(a => a.IsProduct);
            rptProducts.DataBind();
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            ShowQuote();
            // From now on the page shows the job as it is saved now, and the amount just worked out.
            hfShownStatus.Value = Job.CurrentStatus;
            hfShownDifference.Value = Quote != null ? Money(Difference) : "";
        }

        // ------------------------------------------------------------ Save

        private void Save()
        {
            CustomerService.Problems p = CustomerService.Check(txtCustomerName.Text, txtContactNumber.Text, txtEmail.Text, Job.CustomerId);
            if (p.Any)
            {
                if (p.Name != null) ShowFieldError(lblNameError, p.Name);
                if (p.Contact != null) ShowFieldError(lblContactError, p.Contact);
                if (p.Email != null) ShowFieldError(lblEmailError, p.Email);
                ShowNotice("warn", "Nothing was saved. Check the customer's details.");
                return;
            }

            decimal weight;
            if (!TryWeight(out weight))
            {
                ShowNotice("warn", "Nothing was saved. Enter the weight in kilos, for example 5.1.");
                return;
            }

            decimal shown;
            decimal? cash = null;
            if (rbAdjCash.Checked && txtAdjCashReceived.Text.Trim() != "")
            {
                decimal typed;
                if (!decimal.TryParse(txtAdjCashReceived.Text.Replace(",", "").Replace("₱", "").Trim(), NumberStyles.Number, Inv, out typed))
                {
                    ShowNotice("warn", "Nothing was saved. Enter the cash received in pesos.");
                    return;
                }
                cash = typed;
            }

            var request = new JobOrderService.EditRequest
            {
                JobId = Job.JobId,
                ExpectedStatus = hfShownStatus.Value,
                CustomerName = txtCustomerName.Text,
                ContactNumber = txtContactNumber.Text,
                CustomerEmail = txtEmail.Text,
                ServiceId = SelectedServiceId(),
                WeightKg = weight,
                Remarks = txtRemarks.Text,
                Method = rbAdjCash.Checked ? Payment.Cash : Payment.Ewallet,
                CashReceived = cash,
                ShownDifference = decimal.TryParse(hfShownDifference.Value, NumberStyles.Number, Inv, out shown) ? shown : (decimal?)null,
            };
            foreach (var pair in PostedQuantities())
                request.AddOnQuantities[pair.Key] = pair.Value;

            try
            {
                JobOrderService.SaveEdit(request, CurrentUser.UserId);
                Response.Redirect("../Pages/Shared/JobDetail.aspx?id=" + Job.JobId + "&done=edited", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (BusinessRuleException ex)
            {
                ShowNotice("warn", "Nothing was saved. " + ex.Message);
                // The job may have moved on: show it as it is saved now.
                Job = JobOrderRepository.GetById(Job.JobId);
                AmountPaid = PaymentRepository.GetJobTotals(Job.JobId).AmountPaid;
                ddlService.Enabled = ServiceChangeAllowed;
            }
        }

        // ------------------------------------------------------------ The price shown

        /// <summary>Prices what is on the form now, the same way the save will (JobOrderService.QuoteEdit).</summary>
        private void ShowQuote()
        {
            Quote = null;
            decimal weight;
            if (TryWeight(out weight) && weight > 0)
            {
                try
                {
                    Quote = JobOrderService.QuoteEdit(Job, currentLines, SelectedServiceId(), weight, CurrentQuantities(), addOns, services);
                }
                catch (BusinessRuleException)
                {
                    Quote = null;   // the reason was already shown when the save was refused
                }
            }
            Difference = Quote != null ? Quote.Total - AmountPaid : 0m;
            SettleState = Difference > 0 ? "collect" : Difference < 0 ? "refund" : "none";
        }

        private int SelectedServiceId()
        {
            int id;
            return ServiceChangeAllowed && int.TryParse(ddlService.SelectedValue, out id) ? id : Job.ServiceId;
        }

        private bool TryWeight(out decimal weight)
        {
            return decimal.TryParse(txtWeight.Text.Trim().Replace(',', '.'), NumberStyles.Number, Inv, out weight);
        }

        /// <summary>The add-ons as the form holds them: what was posted, or what is on the job when the page opens.</summary>
        private Dictionary<int, int> CurrentQuantities()
        {
            return IsPostBack ? PostedQuantities() : currentLines.ToDictionary(l => l.AddOnId, l => l.Quantity);
        }

        private Dictionary<int, int> PostedQuantities()
        {
            var result = new Dictionary<int, int>();
            string[] ticked = Request.Form.GetValues("addon") ?? new string[0];
            foreach (AddOn a in addOns)
            {
                if (a.IsProduct)
                {
                    int qty;
                    if (int.TryParse(Request.Form["qty_" + a.AddOnId], NumberStyles.None, Inv, out qty) && qty > 0)
                        result[a.AddOnId] = qty;
                }
                else if (ticked.Contains(a.AddOnId.ToString(Inv)))
                {
                    result[a.AddOnId] = 1;
                }
            }
            return result;
        }

        /// <summary>This job's frozen rate for its own service; today's rate for any other.</summary>
        private decimal RateFor(LaundryService s)
        {
            return s.ServiceId == Job.ServiceId ? Job.RatePerKgAtCreation : s.RatePerKg;
        }

        private void ShowNotice(string kind, string text)
        {
            string icon = kind == "warn" ? "circle-alert" : "circle-check";
            litNotice.Text = "<div class=\"notice notice-" + kind + "\">" + Icons.Get(icon, "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        private static void ShowFieldError(Label label, string text)
        {
            label.Text = HttpUtility.HtmlEncode(text);
            label.Visible = true;
        }

        // ---- Used by the markup ----

        protected string SaveLabel
        {
            get
            {
                if (SettleState == "collect") return "Save and collect " + Peso(Difference);
                if (SettleState == "refund") return "Save and refund " + Peso(-Difference);
                return "Save changes";
            }
        }

        protected string RefundText
        {
            get
            {
                if (Quote == null || Difference >= 0) return "";
                return "The customer paid " + Peso(AmountPaid) + " but the corrected total is " + Peso(Quote.Total)
                     + ". Give back " + Peso(-Difference) + " before saving. It is recorded as a −" + Peso(-Difference) + " refund row.";
            }
        }

        /// <summary>The price an add-on bills at on this job: its frozen price if it is already on the job, else today's.</summary>
        protected decimal PriceFor(AddOn a)
        {
            JobAddOn line = currentLines.FirstOrDefault(l => l.AddOnId == a.AddOnId);
            return line != null ? line.PriceAtCreation : a.Price;
        }

        protected bool IsTicked(int addOnId)
        {
            int qty;
            return CurrentQuantities().TryGetValue(addOnId, out qty) && qty > 0;
        }

        protected string QuantityOf(int addOnId)
        {
            int qty;
            return CurrentQuantities().TryGetValue(addOnId, out qty) ? qty.ToString(Inv) : "0";
        }

        protected static string Money(decimal amount)
        {
            return amount.ToString("0.00", Inv);
        }

        protected static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2", Inv);
        }

        protected static string Kg(decimal kg)
        {
            return kg.ToString("0.0#", Inv);
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

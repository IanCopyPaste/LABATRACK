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

namespace LABATRACK.Pages.Shared
{
    // Taking in a load at the counter (staff and owner). The page only collects what was typed;
    // JobOrderService.Create checks every rule, prices the job with PricingService, and saves it
    // all in one transaction. The add-on boxes are plain inputs (name="addon" for the service
    // add-ons, name="qty_<AddOnId>" for the products), read back from the posted form.
    public partial class NewJob : BasePage
    {
        protected override string PageStyleSheet
        {
            get { return "NewJob.css"; }
        }

        protected override string ActiveNav
        {
            get { return "NewJob"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Read by NewJob.js from data- attributes, so the script never has a price written into it.
        protected string RatesData { get; private set; }      // "1:40.00,2:35.00": service id -> rate per kg
        protected string RoutesData { get; private set; }     // "1:WDF,2:WD": service id -> work stages
        protected string MinimumData { get; private set; }
        protected string RoundingData { get; private set; }
        protected string MaxWeightData { get; private set; }

        protected int TurnaroundHours { get; private set; }
        protected bool CustomerFound { get; private set; }
        protected string FoundNote { get; private set; }
        protected string CancelUrl { get; private set; }

        private ShopSettings settings;
        private List<LaundryService> services;
        private List<AddOn> addOns;

        protected void Page_Load(object sender, EventArgs e)
        {
            settings = SettingsRepository.Get();
            services = ServiceRepository.GetAll().Where(s => s.IsActive).ToList();
            addOns = AddOnRepository.GetAll().Where(a => a.IsActive).ToList();

            TurnaroundHours = settings.DefaultTurnaroundHours;
            CancelUrl = ResolveUrl(IsOwner ? "~/Admin/Dashboard.aspx" : "~/Pages/Shared/Dashboard.aspx");
            RatesData = string.Join(",", services.Select(s => s.ServiceId + ":" + s.RatePerKg.ToString("0.00", Inv)));
            RoutesData = string.Join(",", services.Select(s => s.ServiceId + ":" + s.StageCode));
            MinimumData = settings.MinimumCharge.ToString("0.00", Inv);
            RoundingData = settings.RoundingIncrementKg.ToString("0.##", Inv);
            MaxWeightData = settings.MaxWeightKg.ToString("0.##", Inv);

            if (!IsPostBack)
            {
                foreach (LaundryService s in services)
                    ddlService.Items.Add(new ListItem(s.Name + " · ₱" + s.RatePerKg.ToString("N2", Inv) + "/kg", s.ServiceId.ToString(Inv)));
                txtExpectedPickup.Text = DateTime.Now.AddHours(settings.DefaultTurnaroundHours).ToString("yyyy-MM-ddTHH:mm", Inv);
            }
            else if (Request.Form["action"] == "find")
            {
                Find();
            }

            if (services.Count == 0)
            {
                ShowNotice("warn", "There is no service to choose yet. The owner adds services on the Pricing page.");
                btnCreate.Enabled = false;
            }

            rptServiceAddOns.DataSource = addOns.Where(a => !a.IsProduct);
            rptServiceAddOns.DataBind();
            rptProducts.DataSource = addOns.Where(a => a.IsProduct);
            rptProducts.DataBind();
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            ApplyCustomerState();
        }

        // ------------------------------------------------------------ 1. Customer lookup

        // Search by contact number first, and fill in the customer if found. A new customer is
        // created only when nobody has the number (CustomerService, and a unique index in SQL).
        private void Find()
        {
            string contact = txtSearchContact.Text.Trim();
            if (CustomerService.Digits(contact).Length < 7)
            {
                ShowFieldError(lblContactError, "Enter the contact number, for example 0917 555 0123.");
                return;
            }

            Customer found = CustomerService.FindByContact(contact);
            if (found != null)
            {
                UseCustomer(found);
                return;
            }

            // Nobody has this number: the name and email boxes open for a new customer. If they were
            // showing someone found earlier, that person's details are cleared.
            if (hfCustomerId.Value != "")
            {
                txtCustomerName.Text = "";
                txtEmail.Text = "";
            }
            hfCustomerId.Value = "";
            pnlNewCustomer.Visible = true;
        }

        private void UseCustomer(Customer c)
        {
            hfCustomerId.Value = c.CustomerId.ToString(Inv);
            txtCustomerName.Text = c.Name;
            txtEmail.Text = c.Email ?? "";
        }

        // A found customer's name and email are shown read-only: staff add customers but do not
        // edit them (the owner does, on Customers). A read-only box also ignores a posted value.
        private void ApplyCustomerState()
        {
            int id;
            Customer c = int.TryParse(hfCustomerId.Value, out id) ? CustomerRepository.GetById(id) : null;
            CustomerFound = c != null;
            txtCustomerName.ReadOnly = CustomerFound;
            txtEmail.ReadOnly = CustomerFound;
            txtCustomerName.CssClass = CustomerFound ? "input" : "";
            txtEmail.CssClass = CustomerFound ? "input" : "";
            pnlFound.Visible = CustomerFound;
            if (CustomerFound)
            {
                pnlNewCustomer.Visible = false;
                FoundNote = "Details filled in below. " + (c.JobCount == 0 ? "No previous job orders."
                          : c.JobCount + (c.JobCount == 1 ? " previous job order." : " previous job orders."));
            }
        }

        // ------------------------------------------------------------ Save

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            int customerId;
            bool haveCustomer = int.TryParse(hfCustomerId.Value, out customerId);

            if (!haveCustomer)
            {
                // A number that already belongs to someone is used, never added a second time.
                Customer existing = CustomerService.FindByContact(txtSearchContact.Text);
                if (existing != null)
                {
                    UseCustomer(existing);
                    ShowNotice("warn", "This number already belongs to " + existing.Name
                                      + ". Their details are filled in. Check them with the customer, then press Create again.");
                    return;
                }

                CustomerService.Problems p = CustomerService.Check(txtCustomerName.Text, txtSearchContact.Text, txtEmail.Text, null);
                if (p.Any)
                {
                    if (p.Name != null) ShowFieldError(lblNameError, p.Name);
                    if (p.Contact != null) ShowFieldError(lblContactError, p.Contact);
                    if (p.Email != null) ShowFieldError(lblEmailError, p.Email);
                    pnlNewCustomer.Visible = true;
                    ShowError("Check the customer's details above.");
                    return;
                }
            }

            decimal weight;
            if (!decimal.TryParse(txtWeight.Text.Trim().Replace(',', '.'), NumberStyles.Number, Inv, out weight))
            {
                ShowError("Enter the weight in kilos, for example 5.1.");
                return;
            }

            // The cash box is read only for cash. The server checks it against the total it works
            // out itself; the change the page showed is never used.
            decimal? cash = null;
            if (rbCash.Checked)
            {
                decimal typed;
                string text = txtCashReceived.Text.Replace(",", "").Replace("₱", "").Trim();
                if (!decimal.TryParse(text, NumberStyles.Number, Inv, out typed))
                {
                    ShowError("Enter the cash received from the customer.");
                    return;
                }
                cash = typed;
            }

            DateTime? pickup = null;
            if (txtExpectedPickup.Text.Trim() != "")
            {
                DateTime at;
                if (!DateTime.TryParseExact(txtExpectedPickup.Text.Trim(), "yyyy-MM-ddTHH:mm", Inv, DateTimeStyles.None, out at))
                {
                    ShowError("Enter the expected pick-up date and time.");
                    return;
                }
                pickup = at;
            }

            int serviceId;
            int.TryParse(ddlService.SelectedValue, out serviceId);

            var request = new JobOrderService.NewJobRequest
            {
                CustomerId = haveCustomer ? customerId : (int?)null,
                CustomerName = txtCustomerName.Text,
                ContactNumber = txtSearchContact.Text,
                CustomerEmail = txtEmail.Text,
                ServiceId = serviceId,
                WeightKg = weight,
                Remarks = txtRemarks.Text,
                ExpectedPickupAt = pickup,
                Method = rbCash.Checked ? Payment.Cash : Payment.Ewallet,
                CashReceived = cash,
            };
            foreach (var pair in PostedQuantities())
                request.AddOnQuantities[pair.Key] = pair.Value;

            try
            {
                int jobId = JobOrderService.Create(request, CurrentUser.UserId);
                Response.Redirect("JobSlip.aspx?id=" + jobId + "&new=1", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (BusinessRuleException ex)
            {
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// The add-ons as posted: a ticked service add-on is 1, a product is the number typed.
        /// A blank or unreadable quantity counts as none.
        /// </summary>
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

        // ------------------------------------------------------------ Pieces

        private void ShowError(string text)
        {
            lblPaymentError.Text = HttpUtility.HtmlEncode(text);
            lblPaymentError.Visible = true;
        }

        private static void ShowFieldError(Label label, string text)
        {
            label.Text = HttpUtility.HtmlEncode(text);
            label.Visible = true;
        }

        private void ShowNotice(string kind, string text)
        {
            string icon = kind == "warn" ? "circle-alert" : "circle-check";
            litNotice.Text = "<div class=\"notice notice-" + kind + "\">" + Icons.Get(icon, "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        // ---- Used by the markup ----

        protected static string PriceData(decimal price)
        {
            return price.ToString("0.00", Inv);
        }

        /// <summary>A service add-on stays ticked after the page comes back (Find, or a refused save).</summary>
        protected bool IsTicked(int addOnId)
        {
            return IsPostBack && (Request.Form.GetValues("addon") ?? new string[0]).Contains(addOnId.ToString(Inv));
        }

        /// <summary>A product's quantity as last typed, or 0.</summary>
        protected string QuantityOf(int addOnId)
        {
            int qty;
            return IsPostBack && int.TryParse(Request.Form["qty_" + addOnId], NumberStyles.None, Inv, out qty)
                ? qty.ToString(Inv) : "0";
        }
    }
}

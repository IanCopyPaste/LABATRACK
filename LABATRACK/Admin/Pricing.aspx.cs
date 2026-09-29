using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.DataAccess;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Admin
{
    // The price list (owner only): services with their stages, service add-ons, product add-ons,
    // and the pricing rules (minimum charge, rounding step, maximum weight per job).
    // Every save redirects back here with ?saved=..., so a refresh never saves twice.
    public partial class Pricing : AdminBasePage
    {
        protected override string ActiveNav
        {
            get { return "Pricing"; }
        }

        protected override string PageStyleSheet
        {
            get { return "Pricing.css"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // The three work stages a service can include, always in this order. Queued comes before
        // them and Inspection, Ready for pick-up and Claimed after, for every service.
        private static readonly string[] WorkStages = { StatusService.Washing, StatusService.Drying, StatusService.Folding };

        protected string ExampleTitle { get; private set; }
        protected string ExampleBreakdown { get; private set; }

        private List<LaundryService> services;
        private List<AddOn> addOns;

        protected void Page_Load(object sender, EventArgs e)
        {
            services = ServiceRepository.GetAll();
            addOns = AddOnRepository.GetAll();

            if (IsPostBack)
            {
                // Plain submit buttons: action=rules (the rules card) or action=price (the edit window).
                string action = Request.Form["action"];
                if (action == "rules") SaveRules();
                else if (action == "price") SavePrice();
            }
            else
            {
                ShowSavedNotice();
                ShopSettings s = SettingsRepository.Get();
                txtMinimumCharge.Text = s.MinimumCharge.ToString("0.00", Inv);
                ddlRounding.SelectedValue = s.RoundingIncrementKg == 0.5m ? "0.5" : "1";
                txtMaxWeight.Text = s.MaxWeightKg.ToString("0.##", Inv);
                txtMinDownPayment.Text = s.MinimumDownPaymentPercent.ToString(Inv);
            }

            rptServices.DataSource = services;
            rptServices.DataBind();
            rptServiceAddOns.DataSource = addOns.Where(a => !a.IsProduct);
            rptServiceAddOns.DataBind();
            rptProducts.DataSource = addOns.Where(a => a.IsProduct);
            rptProducts.DataBind();
            BuildExample();
        }

        // ------------------------------------------------------------ Pricing rules

        private void SaveRules()
        {
            bool ok = true;
            decimal minimum, maxWeight;
            if (!TryMoney(txtMinimumCharge.Text, out minimum) || minimum < 0)
                ok = ShowError(lblMinimumError, "Enter an amount in pesos, for example 120 or 120.00.");
            if (!decimal.TryParse(txtMaxWeight.Text.Trim(), NumberStyles.Number, Inv, out maxWeight)
                || maxWeight <= 0 || maxWeight > 1000 || maxWeight != Math.Round(maxWeight, 2))
                ok = ShowError(lblMaxWeightError, "Enter a weight in kilos above 0 and at most 1,000, for example 100.");
            int minDown;
            if (!int.TryParse(txtMinDownPayment.Text.Trim().TrimEnd('%').Trim(), NumberStyles.None, Inv, out minDown) || minDown > 100)
                ok = ShowError(lblMinDownError, "Enter a whole percentage from 0 to 100, for example 50.");
            decimal rounding = ddlRounding.SelectedValue == "0.5" ? 0.5m : 1m;
            if (!ok) return;

            SettingsRepository.Set(ShopSettings.MinimumChargeKey, minimum);
            SettingsRepository.Set(ShopSettings.RoundingIncrementKgKey, rounding);
            SettingsRepository.Set(ShopSettings.MaxWeightKgKey, maxWeight);
            SettingsRepository.Set(ShopSettings.MinimumDownPaymentPercentKey, minDown);
            Redirect("rules");
        }

        // ------------------------------------------------------------ The edit window

        // The window posts: pdKind (service, service-addon or product), pdId (empty when adding),
        // pdName, pdPrice, pdActive, and for a service pdWash, pdDry, pdFold. Pricing.js checks them
        // first so the owner sees a mistake at once, but every rule is checked again here.
        private void SavePrice()
        {
            string kind = Request.Form["pdKind"];
            if (kind != "service" && kind != "service-addon" && kind != "product") return;
            int id;
            bool adding = !int.TryParse(Request.Form["pdId"], out id);
            string name = (Request.Form["pdName"] ?? "").Trim();
            bool active = Request.Form["pdActive"] == "1";
            decimal price;
            bool priceOk = TryMoney(Request.Form["pdPrice"], out price) && price > 0 && price <= 99999.99m;

            string problem;
            if (name == "" || name.Length > 60)
                problem = "Enter a name of up to 60 characters.";
            else if (!priceOk)
                problem = "Enter a price in pesos, for example 40 or 42.50.";
            else if (NameTaken(kind, name, adding ? 0 : id))
                problem = "There is already one called \"" + name + "\" in that list.";
            else if (kind == "service")
                problem = SaveService(adding, id, name, price, active);
            else
                problem = SaveAddOn(adding, id, name, price, active, kind == "product" ? AddOn.ProductKind : AddOn.ServiceKind);

            if (problem != null)
                ShowNotice("warn", "Nothing was saved. " + problem);
        }

        private string SaveService(bool adding, int id, string name, decimal rate, bool active)
        {
            var s = new LaundryService
            {
                ServiceId = id,
                Name = name,
                RatePerKg = rate,
                IsActive = active,
                IncludesWashing = Request.Form["pdWash"] == "1",
                IncludesDrying = Request.Form["pdDry"] == "1",
                IncludesFolding = Request.Form["pdFold"] == "1",
            };
            // A service with no work stage would go straight from Queued to Inspection.
            if (!s.IncludesWashing && !s.IncludesDrying && !s.IncludesFolding)
                return "Tick at least one stage.";

            if (adding)
            {
                id = ServiceRepository.Insert(s);
            }
            else
            {
                // A service with a load still in the shop cannot be changed at all (name, rate,
                // stages or Active). The procedure checks this in the same statement as the update.
                LaundryService current = services.FirstOrDefault(x => x.ServiceId == id);
                if (current == null) return "That service could not be found.";
                if (current.IsInUse || !ServiceRepository.Update(s))
                    return current.Name + " has a load in the shop, so it can't be changed until that load is claimed or voided. Add a new service instead.";
            }
            Redirect("service-" + id);
            return null;
        }

        private string SaveAddOn(bool adding, int id, string name, decimal price, bool active, string addOnKind)
        {
            var a = new AddOn { AddOnId = id, Name = name, Price = price, IsActive = active, Kind = addOnKind };
            if (adding)
            {
                id = AddOnRepository.Insert(a);
            }
            else
            {
                AddOn current = addOns.FirstOrDefault(x => x.AddOnId == id);
                if (current == null || current.Kind != addOnKind) return "That add-on could not be found.";
                AddOnRepository.Update(a);
            }
            Redirect((addOnKind == AddOn.ProductKind ? "product-" : "service-addon-") + id);
            return null;
        }

        // Two items with the same name in one list would be impossible to tell apart at the counter.
        private bool NameTaken(string kind, string name, int exceptId)
        {
            if (kind == "service")
                return services.Any(s => s.ServiceId != exceptId && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            string addOnKind = kind == "product" ? AddOn.ProductKind : AddOn.ServiceKind;
            return addOns.Any(a => a.Kind == addOnKind && a.AddOnId != exceptId && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        // ------------------------------------------------------------ The worked example

        /// <summary>
        /// "How a total is computed", priced by PricingService with today's prices: the first active
        /// service, 5.1 kg, the first service add-on and two of the first product.
        /// </summary>
        private void BuildExample()
        {
            ShopSettings s = SettingsRepository.Get();
            LaundryService service = services.FirstOrDefault(x => x.IsActive);
            if (service == null)
            {
                ExampleTitle = "Add a service to see an example";
                ExampleBreakdown = "<p class=\"muted small\">There is no active service yet.</p>";
                return;
            }

            var quantities = new Dictionary<int, int>();
            AddOn serviceAddOn = addOns.FirstOrDefault(a => a.IsActive && !a.IsProduct);
            AddOn product = addOns.FirstOrDefault(a => a.IsActive && a.IsProduct);
            if (serviceAddOn != null) quantities[serviceAddOn.AddOnId] = 1;
            if (product != null) quantities[product.AddOnId] = 2;
            PriceQuote q = JobOrderService.QuoteNew(service, 5.1m, quantities, addOns, s);

            ExampleTitle = "Example: " + service.Name + ", 5.1 kg"
                         + (serviceAddOn != null || product != null ? ", with add-ons" : "");
            var sb = new StringBuilder("<dl class=\"dl\">");
            Line(sb, "Weight", Kg(q.WeightKg), false);
            Line(sb, "Billable (rounded up)", Kg(q.BillableKg), false);
            Line(sb, Kg(q.BillableKg) + " × " + Peso(q.RatePerKg), Peso(q.ChargeBeforeMinimum), false);
            Line(sb, "Minimum charge check", q.MinimumApplied
                ? "raised to " + Peso(q.MinimumCharge)
                : Peso(q.ChargeBeforeMinimum) + " ≥ " + Peso(q.MinimumCharge), !q.MinimumApplied);
            foreach (PriceLine line in q.Lines)
                Line(sb, line.Label, Peso(line.Amount), false);
            sb.Append("</dl><div class=\"divider\" style=\"margin:14px 0;\"></div>")
              .Append("<div class=\"total-line\"><span>Total</span><span class=\"num\">").Append(Peso(q.Total)).Append("</span></div>");
            ExampleBreakdown = sb.ToString();
        }

        private static void Line(StringBuilder sb, string label, string value, bool muted)
        {
            sb.Append("<dt>").Append(HttpUtility.HtmlEncode(label)).Append("</dt><dd")
              .Append(muted ? " class=\"muted\"" : "").Append(">").Append(HttpUtility.HtmlEncode(value)).Append("</dd>");
        }

        // ------------------------------------------------------------ Pieces

        private void Redirect(string saved)
        {
            Response.Redirect("Pricing.aspx?saved=" + saved, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void ShowSavedNotice()
        {
            string saved = Request.QueryString["saved"];
            if (saved == "rules")
                ShowNotice("info", "Pricing rules saved. They apply to new job orders only.");
            else if (!string.IsNullOrEmpty(saved))
                ShowNotice("info", "Saved. The new price applies to new job orders only; jobs already made keep theirs.");
        }

        private void ShowNotice(string kind, string text)
        {
            string icon = kind == "warn" ? "circle-alert" : "circle-check";
            litNotice.Text = "<div class=\"notice notice-" + kind + "\">" + Icons.Get(icon, "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        // Money is DECIMAL(10,2): digits, at most two places after the point.
        private static bool TryMoney(string text, out decimal amount)
        {
            text = (text ?? "").Replace(",", "").Replace("₱", "").Trim();
            return decimal.TryParse(text, NumberStyles.Number, Inv, out amount) && amount == Math.Round(amount, 2);
        }

        // Always returns false, so a failed check reads as ok = ShowError(...).
        private static bool ShowError(Label label, string text)
        {
            label.Text = HttpUtility.HtmlEncode(text);
            label.Visible = true;
            return false;
        }

        private static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2", Inv);
        }

        private static string Kg(decimal kg)
        {
            return kg.ToString("0.##", Inv) + " kg";
        }

        // ---- Used by the markup ----

        /// <summary>The row just saved is washed in the accent for a moment.</summary>
        protected string RowCss(string kind, int id)
        {
            return Request.QueryString["saved"] == kind + "-" + id ? "is-changed" : "";
        }

        /// <summary>All three stages as chips; the ones this service skips are faded and struck through.</summary>
        protected static string StageChips(string code)
        {
            var sb = new StringBuilder();
            foreach (string stage in WorkStages)
            {
                bool on = code.IndexOf(stage[0]) >= 0;
                sb.Append("<span class=\"stage-chip").Append(on ? "" : " is-off").Append("\"")
                  .Append(on ? "" : " title=\"Skipped by this service\"").Append(">")
                  .Append(HttpUtility.HtmlEncode(stage)).Append("</span>");
            }
            return sb.ToString();
        }
    }
}

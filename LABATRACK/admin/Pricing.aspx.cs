using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.db_conn;
using LABATRACK.tools;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.admin
{
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

        private static readonly string[] WorkStages = { JobStatus.Washing, JobStatus.Drying, JobStatus.Folding };

        protected string ExampleTitle { get; private set; }
        protected string ExampleBreakdown { get; private set; }

        private List<LaundryService> services = new List<LaundryService>();
        private List<AddOn> addOns = new List<AddOn>();
        private ShopSettings settings = new ShopSettings();

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadServices();
            LoadAddOns();
            LoadSettings();

            if (IsPostBack)
            {
                string action = Request.Form["action"];
                if (action == "rules") SaveRules();
                else if (action == "price") SavePrice();
            }
            else
            {
                ShowSavedNotice();
                txtMinimumCharge.Text = settings.MinimumCharge.ToString("0.00", Inv);
                txtMaxWeight.Text = settings.MaxWeightKg.ToString("0.##", Inv);
                txtMinDownPayment.Text = settings.MinimumDownPaymentPercent.ToString(Inv);
            }

            rptServices.DataSource = services;
            rptServices.DataBind();
            rptServiceAddOns.DataSource = addOns.Where(a => !a.IsProduct);
            rptServiceAddOns.DataBind();
            rptProducts.DataSource = addOns.Where(a => a.IsProduct);
            rptProducts.DataBind();
            BuildExample();
        }

        private void LoadServices()
        {
            services.Clear();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Services_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        LaundryService service = new LaundryService();
                        service.ServiceId = (int)r["ServiceId"];
                        service.Name = (string)r["Name"];
                        service.RatePerKg = (decimal)r["RatePerKg"];
                        service.IncludesWashing = (bool)r["IncludesWashing"];
                        service.IncludesDrying = (bool)r["IncludesDrying"];
                        service.IncludesFolding = (bool)r["IncludesFolding"];
                        service.IsActive = (bool)r["IsActive"];
                        service.LoadsInShop = Convert.ToInt32(r["LoadsInShop"]);
                        services.Add(service);
                    }
                }
            }
        }

        private void LoadAddOns()
        {
            addOns.Clear();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_AddOns_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        AddOn addOn = new AddOn();
                        addOn.AddOnId = (int)r["AddOnId"];
                        addOn.Name = (string)r["Name"];
                        addOn.Price = (decimal)r["Price"];
                        addOn.Kind = (string)r["Kind"];
                        addOn.IsActive = (bool)r["IsActive"];
                        addOns.Add(addOn);
                    }
                }
            }
        }

        private void LoadSettings()
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string key = (string)r["SettingKey"];
                        string value = r["SettingValue"] as string;
                        if (value == null) continue;

                        decimal money;
                        int number;
                        if (key == ShopSettings.MinimumChargeKey && decimal.TryParse(value, NumberStyles.Number, Inv, out money))
                            settings.MinimumCharge = money;
                        else if (key == ShopSettings.MaxWeightKgKey && decimal.TryParse(value, NumberStyles.Number, Inv, out money))
                            settings.MaxWeightKg = money;
                        else if (key == ShopSettings.MinimumDownPaymentPercentKey && int.TryParse(value, NumberStyles.Integer, Inv, out number))
                            settings.MinimumDownPaymentPercent = Math.Min(100, Math.Max(0, number));
                    }
                }
            }
        }

        private void SaveRules()
        {
            bool ok = true;
            decimal minimum, maxWeight;
            int minDown;

            if (!TryMoney(txtMinimumCharge.Text, out minimum) || minimum < 0)
            {
                ShowError(lblMinimumError, "Enter an amount in pesos, for example 120 or 120.00.");
                ok = false;
            }
            if (!decimal.TryParse(txtMaxWeight.Text.Trim(), NumberStyles.Number, Inv, out maxWeight)
                || maxWeight <= 0 || maxWeight > 1000 || maxWeight != Math.Round(maxWeight, 2))
            {
                ShowError(lblMaxWeightError, "Enter a weight in kilos above 0 and at most 1,000, for example 100.");
                ok = false;
            }
            if (!int.TryParse(txtMinDownPayment.Text.Trim().TrimEnd('%').Trim(), NumberStyles.None, Inv, out minDown) || minDown > 100)
            {
                ShowError(lblMinDownError, "Enter a whole percentage from 0 to 100, for example 50.");
                ok = false;
            }
            if (!ok) return;

            SaveSetting(ShopSettings.MinimumChargeKey, minimum.ToString("0.00##", Inv));
            SaveSetting(ShopSettings.MaxWeightKgKey, maxWeight.ToString("0.00##", Inv));
            SaveSetting(ShopSettings.MinimumDownPaymentPercentKey, minDown.ToString(Inv));
            Redirect("rules");
        }

        private void SaveSetting(string key, string value)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_Set", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@SettingKey", SqlDbType.NVarChar, 50).Value = key;
                cmd.Parameters.Add("@SettingValue", SqlDbType.NVarChar, 400).Value = value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

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
            bool washing = Request.Form["pdWash"] == "1";
            bool drying = Request.Form["pdDry"] == "1";
            bool folding = Request.Form["pdFold"] == "1";

            if (!washing && !drying && !folding)
                return "Tick at least one stage.";

            if (adding)
            {
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Services_Insert", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 60).Value = name;
                    cmd.Parameters.Add(new SqlParameter("@RatePerKg", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = rate });
                    cmd.Parameters.Add("@IncludesWashing", SqlDbType.Bit).Value = washing;
                    cmd.Parameters.Add("@IncludesDrying", SqlDbType.Bit).Value = drying;
                    cmd.Parameters.Add("@IncludesFolding", SqlDbType.Bit).Value = folding;
                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = active;

                    conn.Open();
                    id = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            else
            {
                LaundryService current = services.FirstOrDefault(x => x.ServiceId == id);
                if (current == null) return "That service could not be found.";

                int rowsAffected = 0;
                if (current.IsInUse == false)
                {
                    using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                    using (SqlCommand cmd = new SqlCommand("dbo.usp_Services_Update", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = id;
                        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 60).Value = name;
                        cmd.Parameters.Add(new SqlParameter("@RatePerKg", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = rate });
                        cmd.Parameters.Add("@IncludesWashing", SqlDbType.Bit).Value = washing;
                        cmd.Parameters.Add("@IncludesDrying", SqlDbType.Bit).Value = drying;
                        cmd.Parameters.Add("@IncludesFolding", SqlDbType.Bit).Value = folding;
                        cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = active;

                        conn.Open();
                        rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }

                if (rowsAffected != 1)
                    return current.Name + " has a load in the shop, so it can't be changed until that load is claimed or voided. Add a new service instead.";
            }

            Redirect("service-" + id);
            return null;
        }

        private string SaveAddOn(bool adding, int id, string name, decimal price, bool active, string addOnKind)
        {
            if (adding)
            {
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_AddOns_Insert", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 60).Value = name;
                    cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = price });
                    cmd.Parameters.Add("@Kind", SqlDbType.NVarChar, 10).Value = addOnKind;
                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = active;

                    conn.Open();
                    id = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            else
            {
                AddOn current = addOns.FirstOrDefault(x => x.AddOnId == id);
                if (current == null || current.Kind != addOnKind) return "That add-on could not be found.";

                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_AddOns_Update", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@AddOnId", SqlDbType.Int).Value = id;
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 60).Value = name;
                    cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = price });
                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = active;

                    conn.Open();
                    cmd.ExecuteScalar();
                }
            }

            Redirect((addOnKind == AddOn.ProductKind ? "product-" : "service-addon-") + id);
            return null;
        }

        private bool NameTaken(string kind, string name, int exceptId)
        {
            if (kind == "service")
                return services.Any(s => s.ServiceId != exceptId && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            string addOnKind;
            if (kind == "product")
                addOnKind = AddOn.ProductKind;
            else
                addOnKind = AddOn.ServiceKind;
            return addOns.Any(a => a.Kind == addOnKind && a.AddOnId != exceptId && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private void BuildExample()
        {
            LaundryService service = services.FirstOrDefault(x => x.IsActive);
            if (service == null)
            {
                ExampleTitle = "Add a service to see an example";
                ExampleBreakdown = "<p class=\"muted small\">There is no active service yet.</p>";
                return;
            }

            AddOn serviceAddOn = addOns.FirstOrDefault(a => a.IsActive && !a.IsProduct);
            AddOn product = addOns.FirstOrDefault(a => a.IsActive && a.IsProduct);

            decimal weight = 5.1m;
            decimal chargeBeforeMinimum = Math.Round(weight * service.RatePerKg, 2, MidpointRounding.AwayFromZero);
            bool minimumApplied = chargeBeforeMinimum < settings.MinimumCharge;
            decimal laundryCharge;
            if (minimumApplied)
                laundryCharge = settings.MinimumCharge;
            else
                laundryCharge = chargeBeforeMinimum;
            decimal total = laundryCharge;

            ExampleTitle = "Example: " + service.Name + ", 5.1 kg"
                         + (serviceAddOn != null || product != null ? ", with add-ons" : "");

            StringBuilder sb = new StringBuilder("<dl class=\"dl\">");
            Line(sb, "Weight", Kg(weight), false);
            Line(sb, Kg(weight) + " × " + Peso(service.RatePerKg), Peso(chargeBeforeMinimum), false);
            Line(sb, "Minimum charge check", minimumApplied
                ? "raised to " + Peso(settings.MinimumCharge)
                : Peso(chargeBeforeMinimum) + " ≥ " + Peso(settings.MinimumCharge), !minimumApplied);

            if (serviceAddOn != null)
            {
                decimal amount = Math.Round(1 * serviceAddOn.Price, 2, MidpointRounding.AwayFromZero);
                Line(sb, serviceAddOn.Name, Peso(amount), false);
                total += amount;
            }
            if (product != null)
            {
                decimal amount = Math.Round(2 * product.Price, 2, MidpointRounding.AwayFromZero);
                Line(sb, product.Name + " × 2", Peso(amount), false);
                total += amount;
            }

            sb.Append("</dl><div class=\"divider\" style=\"margin:14px 0;\"></div>")
              .Append("<div class=\"total-line\"><span>Total</span><span class=\"num\">").Append(Peso(total)).Append("</span></div>");
            ExampleBreakdown = sb.ToString();
        }

        private static void Line(StringBuilder sb, string label, string value, bool muted)
        {
            sb.Append("<dt>").Append(HttpUtility.HtmlEncode(label)).Append("</dt><dd")
              .Append(muted ? " class=\"muted\"" : "").Append(">").Append(HttpUtility.HtmlEncode(value)).Append("</dd>");
        }

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
            string icon;
            if (kind == "warn")
                icon = "circle-alert";
            else
                icon = "circle-check";
            litNotice.Text = "<div class=\"notice notice-" + kind + "\">" + Icons.Get(icon, "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        private static bool TryMoney(string text, out decimal amount)
        {
            text = (text ?? "").Replace(",", "").Replace("₱", "").Trim();
            return decimal.TryParse(text, NumberStyles.Number, Inv, out amount) && amount == Math.Round(amount, 2);
        }

        private void ShowError(Label label, string text)
        {
            label.Text = HttpUtility.HtmlEncode(text);
            label.Visible = true;
        }

        private static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2", Inv);
        }

        private static string Kg(decimal kg)
        {
            return kg.ToString("0.##", Inv) + " kg";
        }

        protected string RowCss(string kind, int id)
        {
            if (Request.QueryString["saved"] == kind + "-" + id)
                return "is-changed";
            else
                return "";
        }

        protected static string StageChips(string code)
        {
            StringBuilder sb = new StringBuilder();
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

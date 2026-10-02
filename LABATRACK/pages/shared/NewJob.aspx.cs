using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.db_conn;
using LABATRACK.tools;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.pages.shared
{
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

        protected string RatesData { get; private set; }
        protected string RoutesData { get; private set; }
        protected string MinimumData { get; private set; }
        protected string MaxWeightData { get; private set; }

        protected int TurnaroundHours { get; private set; }

        protected int MinDownPercent { get; private set; }

        protected string PaymentSub
        {
            get
            {
                if (MinDownPercent >= 100) return "Full payment is collected now.";
                if (MinDownPercent == 0) return "Paid in full, in part, or at pick-up. The balance is collected before the load is handed over.";
                return "Paid in full, or at least " + MinDownPercent + "% now. The balance is collected before the load is handed over.";
            }
        }
        protected bool CustomerFound { get; private set; }
        protected string FoundNote { get; private set; }
        protected string CancelUrl { get; private set; }

        private ShopSettings settings = new ShopSettings();
        private List<LaundryService> services = new List<LaundryService>();
        private List<AddOn> addOns = new List<AddOn>();

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadSettings();
            LoadServicesAndAddOns();
            services = services.Where(s => s.IsActive).ToList();
            addOns = addOns.Where(a => a.IsActive).ToList();

            TurnaroundHours = settings.DefaultTurnaroundHours;
            MinDownPercent = settings.MinimumDownPaymentPercent;
            CancelUrl = ResolveUrl(IsOwner ? "~/admin/Dashboard.aspx" : "~/pages/shared/Dashboard.aspx");
            RatesData = string.Join(",", services.Select(s => s.ServiceId + ":" + s.RatePerKg.ToString("0.00", Inv)));
            RoutesData = string.Join(",", services.Select(s => s.ServiceId + ":" + s.StageCode));
            MinimumData = settings.MinimumCharge.ToString("0.00", Inv);
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

            List<Customer> everyone = new List<Customer>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Search", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 100).Value = DBNull.Value;
                cmd.Parameters.Add("@Digits", SqlDbType.NVarChar, 20).Value = DBNull.Value;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        everyone.Add(CustomerFromRow(r));
                    }
                }
            }
            rptSuggest.DataSource = everyone.OrderByDescending(c => c.LastDropOff ?? c.CreatedAt).ThenBy(c => c.Name).ToList();
            rptSuggest.DataBind();
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            ApplyCustomerState();
        }

        private void Find()
        {
            string contact = txtSearchContact.Text.Trim();
            if (DigitsOf(contact).Length < 7)
            {
                ShowFieldError(lblContactError, "Enter the contact number, for example 0917 555 0123.");
                return;
            }

            Customer found = FindByDigits(DigitsOf(contact));
            if (found != null)
            {
                UseCustomer(found);
                return;
            }

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

        private void ApplyCustomerState()
        {
            int id;
            Customer c = int.TryParse(hfCustomerId.Value, out id) ? FindById(id) : null;
            CustomerFound = c != null;
            txtCustomerName.ReadOnly = CustomerFound;
            txtEmail.ReadOnly = CustomerFound;
            if (CustomerFound)
                txtCustomerName.CssClass = "input";
            else
                txtCustomerName.CssClass = "";
            if (CustomerFound)
                txtEmail.CssClass = "input";
            else
                txtEmail.CssClass = "";
            pnlFound.Visible = CustomerFound;
            if (CustomerFound)
            {
                pnlNewCustomer.Visible = false;
                FoundNote = "Details filled in below. " + (c.JobCount == 0 ? "No previous job orders."
                          : c.JobCount + (c.JobCount == 1 ? " previous job order." : " previous job orders."));
            }
        }

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            int customerId;
            bool haveCustomer = int.TryParse(hfCustomerId.Value, out customerId);

            if (!haveCustomer)
            {
                Customer existing = FindByDigits(DigitsOf(txtSearchContact.Text));
                if (existing != null)
                {
                    UseCustomer(existing);
                    ShowNotice("warn", "This number already belongs to " + existing.Name
                                      + ". Their details are filled in. Check them with the customer, then press Create again.");
                    return;
                }

                string nameProblem, contactProblem, emailProblem;
                CheckCustomer(txtCustomerName.Text, txtSearchContact.Text, txtEmail.Text, out nameProblem, out contactProblem, out emailProblem);
                if (nameProblem != null || contactProblem != null || emailProblem != null)
                {
                    if (nameProblem != null) ShowFieldError(lblNameError, nameProblem);
                    if (contactProblem != null) ShowFieldError(lblContactError, contactProblem);
                    if (emailProblem != null) ShowFieldError(lblEmailError, emailProblem);
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

            decimal? amountNow = null;
            if (rbPayPart.Checked)
            {
                decimal typed;
                if (!TryPesos(txtDownPayment.Text, out typed))
                {
                    ShowError("Enter the down payment in pesos, for example 150 or 150.00.");
                    return;
                }
                amountNow = typed;
            }
            else if (rbPayLater.Checked)
            {
                amountNow = 0m;
            }

            decimal? cash = null;
            if (rbCash.Checked && amountNow != 0m)
            {
                decimal typed;
                if (!TryPesos(txtCashReceived.Text, out typed))
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

            int newJobId;
            string error = CreateJob(haveCustomer ? customerId : (int?)null, weight, serviceId, amountNow, cash, pickup, out newJobId);
            if (error != null)
            {
                ShowError(error);
                return;
            }

            Email.SendDropOff(newJobId, CurrentUser.UserId, false);
            Response.Redirect("JobSlip.aspx?id=" + newJobId + "&new=1", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private string CreateJob(int? customerId, decimal weight, int serviceId, decimal? requestedAmountNow, decimal? cashReceived,
                                 DateTime? requestedPickup, out int jobId)
        {
            jobId = 0;
            string method;
            if (rbCash.Checked)
                method = Payment.Cash;
            else
                method = Payment.Ewallet;
            int userId = CurrentUser.UserId;

            LaundryService service = null;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Services_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = serviceId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        service = new LaundryService();
                        service.ServiceId = (int)r["ServiceId"];
                        service.Name = (string)r["Name"];
                        service.RatePerKg = (decimal)r["RatePerKg"];
                        service.IncludesWashing = (bool)r["IncludesWashing"];
                        service.IncludesDrying = (bool)r["IncludesDrying"];
                        service.IncludesFolding = (bool)r["IncludesFolding"];
                        service.IsActive = (bool)r["IsActive"];
                    }
                }
            }
            if (service == null || !service.IsActive)
                return "Choose a service.";

            if (weight <= 0)
                return "Enter the weight in kilos, for example 5.1.";
            if (weight != Math.Round(weight, 2))
                return "Enter the weight with at most two decimals, for example 5.25.";
            if (weight > settings.MaxWeightKg)
                return "One job can be at most " + settings.MaxWeightKg.ToString("0.##") + " kg. Split a bigger load into two or more jobs.";

            List<AddOn> everyAddOn = new List<AddOn>();
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
                        everyAddOn.Add(addOn);
                    }
                }
            }

            Dictionary<int, int> quantities = PostedQuantities();
            List<PriceLine> lines = new List<PriceLine>();
            foreach (AddOn a in everyAddOn)
            {
                int qty;
                if (!quantities.TryGetValue(a.AddOnId, out qty) || qty <= 0) continue;
                if (!a.IsActive)
                    return a.Name + " is no longer offered. Untick it and try again.";

                PriceLine line = new PriceLine();
                line.AddOnId = a.AddOnId;
                line.Name = a.Name;
                line.IsProduct = a.IsProduct;
                line.Quantity = qty;
                line.UnitPrice = a.Price;
                line.Amount = Math.Round(qty * a.Price, 2, MidpointRounding.AwayFromZero);
                lines.Add(line);
            }
            foreach (int id in quantities.Keys)
            {
                if (everyAddOn.All(a => a.AddOnId != id))
                    return "One of the add-ons is not on the price list any more. Reload the page.";
            }
            foreach (PriceLine line in lines)
            {
                if (!line.IsProduct && line.Quantity != 1)
                    return line.Name + " is charged once per job.";
                if (line.IsProduct && line.Quantity > 99)
                    return "Enter at most 99 of " + line.Name + ".";
            }

            decimal chargeBeforeMinimum = Math.Round(weight * service.RatePerKg, 2, MidpointRounding.AwayFromZero);
            decimal laundryCharge;
            if (chargeBeforeMinimum < settings.MinimumCharge)
                laundryCharge = settings.MinimumCharge;
            else
                laundryCharge = chargeBeforeMinimum;
            decimal total = laundryCharge + lines.Sum(l => l.Amount);

            decimal amountNow = requestedAmountNow ?? total;
            if (amountNow < 0 || amountNow != Math.Round(amountNow, 2) || amountNow > 99999999m)
                return "Enter the amount paid now in pesos, for example 150 or 150.00.";
            if (amountNow > total)
                return "The amount paid now can't be more than the " + Peso(total) + " total.";
            decimal minimumDown = Math.Ceiling(total * settings.MinimumDownPaymentPercent) / 100m;
            if (amountNow < minimumDown)
                return "The down payment must be at least " + Peso(minimumDown)
                     + " (" + settings.MinimumDownPaymentPercent + "% of the " + Peso(total) + " total).";

            decimal? cashToStore = null;
            if (amountNow > 0)
            {
                if (method == Payment.Ewallet)
                    cashToStore = null;
                else if (method != Payment.Cash)
                    return "Choose how the customer paid: Cash or E-wallet.";
                else if (!cashReceived.HasValue || cashReceived.Value < amountNow)
                    return "Cash received must be at least the " + Peso(amountNow) + " being paid.";
                else if (cashReceived.Value < 0 || cashReceived.Value != Math.Round(cashReceived.Value, 2) || cashReceived.Value > 99999999m)
                    return "Enter the cash received in pesos, for example 500 or 500.00.";
                else
                    cashToStore = cashReceived.Value;
            }

            if (customerId.HasValue)
            {
                Customer existing = FindById(customerId.Value);
                if (existing == null || DigitsOf(existing.ContactNumber) != DigitsOf(txtSearchContact.Text))
                    return "The contact number was changed after the customer was found. Press Find again.";
            }
            else
            {
                Customer sameNumber = FindByDigits(DigitsOf(txtSearchContact.Text));
                if (sameNumber != null)
                    return "This number already belongs to " + sameNumber.Name + ". Press Find to use their record.";

                string nameProblem, contactProblem, emailProblem;
                CheckCustomer(txtCustomerName.Text, txtSearchContact.Text, txtEmail.Text, out nameProblem, out contactProblem, out emailProblem);
                if (nameProblem != null) return nameProblem;
                if (contactProblem != null) return contactProblem;
                if (emailProblem != null) return emailProblem;
            }

            DateTime now = DateTime.Now;
            DateTime pickup = requestedPickup ?? now.AddHours(settings.DefaultTurnaroundHours);
            if (pickup <= now)
                return "The expected pick-up must be later than now.";

            string remarks = (txtRemarks.Text ?? "").Trim();
            if (remarks.Length > 500)
                return "Keep the remarks to 500 characters.";
            if (remarks == "")
                remarks = null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    int customerIdToUse;
                    if (customerId.HasValue)
                    {
                        customerIdToUse = customerId.Value;
                    }
                    else
                    {
                        string email = (txtEmail.Text ?? "").Trim();
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Insert", conn, tx))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = txtCustomerName.Text.Trim();
                                cmd.Parameters.Add("@ContactNumber", SqlDbType.NVarChar, 20).Value = txtSearchContact.Text.Trim();
                                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = email == "" ? (object)DBNull.Value : email;

                                customerIdToUse = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }
                        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                        {
                            return "This number was just added for another customer. Press Find to use their record.";
                        }
                    }

                    string prefix = "LT-" + now.ToString("yyMMdd", Inv) + "-";
                    string lastClaim = null;
                    using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetLastClaimNumber", conn, tx))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@Prefix", SqlDbType.NVarChar, 12).Value = prefix;

                        object last = cmd.ExecuteScalar();
                        if (last != null && last != DBNull.Value)
                            lastClaim = (string)last;
                    }
                    int number = 0;
                    if (lastClaim != null)
                        int.TryParse(lastClaim.Substring(prefix.Length), NumberStyles.None, Inv, out number);
                    string claimNumber = prefix + (number + 1).ToString("000", Inv);

                    using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_Insert", conn, tx))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@ClaimNumber", SqlDbType.NVarChar, 20).Value = claimNumber;
                        cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerIdToUse;
                        cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = service.ServiceId;
                        cmd.Parameters.Add(new SqlParameter("@WeightKg", SqlDbType.Decimal) { Precision = 6, Scale = 2, Value = weight });
                        cmd.Parameters.Add(new SqlParameter("@RatePerKgAtCreation", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = service.RatePerKg });
                        cmd.Parameters.Add(new SqlParameter("@MinimumChargeAtCreation", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = settings.MinimumCharge });
                        cmd.Parameters.Add(new SqlParameter("@LaundryCharge", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = laundryCharge });
                        cmd.Parameters.Add("@IncludesWashing", SqlDbType.Bit).Value = service.IncludesWashing;
                        cmd.Parameters.Add("@IncludesDrying", SqlDbType.Bit).Value = service.IncludesDrying;
                        cmd.Parameters.Add("@IncludesFolding", SqlDbType.Bit).Value = service.IncludesFolding;
                        cmd.Parameters.Add(new SqlParameter("@TotalAmount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = total });
                        cmd.Parameters.Add("@CurrentStatus", SqlDbType.NVarChar, 20).Value = JobStatus.Queued;
                        cmd.Parameters.Add("@Remarks", SqlDbType.NVarChar, 500).Value = remarks == null ? (object)DBNull.Value : remarks;
                        cmd.Parameters.Add("@ExpectedPickupAt", SqlDbType.DateTime2).Value = pickup;
                        cmd.Parameters.Add("@CreatedByUserId", SqlDbType.Int).Value = userId;

                        jobId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    foreach (PriceLine line in lines)
                    {
                        using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_Insert", conn, tx))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                            cmd.Parameters.Add("@AddOnId", SqlDbType.Int).Value = line.AddOnId;
                            cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = line.Quantity;
                            cmd.Parameters.Add(new SqlParameter("@PriceAtCreation", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = line.UnitPrice });

                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand("dbo.usp_JobStatusHistory_Insert", conn, tx))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                        cmd.Parameters.Add("@FromStatus", SqlDbType.NVarChar, 20).Value = DBNull.Value;
                        cmd.Parameters.Add("@ToStatus", SqlDbType.NVarChar, 20).Value = JobStatus.Queued;
                        cmd.Parameters.Add("@ChangedByUserId", SqlDbType.Int).Value = userId;
                        cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = DBNull.Value;

                        cmd.ExecuteNonQuery();
                    }

                    if (amountNow > 0)
                    {
                        using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_Insert", conn, tx))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                            cmd.Parameters.Add("@Kind", SqlDbType.NVarChar, 12).Value = Payment.DropOff;
                            cmd.Parameters.Add(new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = amountNow });
                            cmd.Parameters.Add("@Method", SqlDbType.NVarChar, 10).Value = method;
                            cmd.Parameters.Add(new SqlParameter("@CashReceived", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = cashToStore.HasValue ? (object)cashToStore.Value : DBNull.Value });
                            cmd.Parameters.Add("@RecordedByUserId", SqlDbType.Int).Value = userId;

                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
            return null;
        }

        private void CheckCustomer(string name, string contact, string email,
                                   out string nameProblem, out string contactProblem, out string emailProblem)
        {
            nameProblem = null;
            contactProblem = null;
            emailProblem = null;
            name = (name ?? "").Trim();
            contact = (contact ?? "").Trim();
            email = (email ?? "").Trim();

            if (name == "")
                nameProblem = "Enter the customer's name.";
            else if (name.Length > 100)
                nameProblem = "Keep the name to 100 characters.";

            string digits = DigitsOf(contact);
            if (contact == "")
                contactProblem = "Enter a contact number. It is how the customer is found next time.";
            else if (contact.Length > 20 || digits.Length < 7 || digits.Length > 15
                     || contact.Any(ch => !char.IsDigit(ch) && " +-()".IndexOf(ch) < 0))
                contactProblem = "Use digits only, for example 0917 555 0123.";
            else
            {
                Customer owner = FindByDigits(digits);
                if (owner != null)
                    contactProblem = "This number already belongs to " + owner.Name + ". Search for it instead of adding a new customer.";
            }

            if (email != "" && !Email.IsValidAddress(email))
                emailProblem = "That does not look like an email address. Leave it blank if they have none.";
        }

        private Dictionary<int, int> PostedQuantities()
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
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
                        else if (key == ShopSettings.DefaultTurnaroundHoursKey && int.TryParse(value, NumberStyles.Integer, Inv, out number))
                            settings.DefaultTurnaroundHours = number;
                    }
                }
            }
        }

        private void LoadServicesAndAddOns()
        {
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

        private Customer FindByDigits(string digits)
        {
            if (digits.Length == 0) return null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_GetByContactDigits", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ContactDigits", SqlDbType.NVarChar, 20).Value = digits;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return CustomerFromRow(r);
                    return null;
                }
            }
        }

        private Customer FindById(int customerId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return CustomerFromRow(r);
                    return null;
                }
            }
        }

        private static Customer CustomerFromRow(SqlDataReader r)
        {
            Customer customer = new Customer();
            customer.CustomerId = (int)r["CustomerId"];
            customer.Name = (string)r["Name"];
            customer.ContactNumber = (string)r["ContactNumber"];
            customer.Email = r["Email"] as string;
            customer.CreatedAt = (DateTime)r["CreatedAt"];
            customer.JobCount = Convert.ToInt32(r["JobCount"]);
            customer.LastDropOff = r["LastDropOff"] as DateTime?;
            return customer;
        }

        private static bool TryPesos(string text, out decimal amount)
        {
            text = (text ?? "").Replace(",", "").Replace("₱", "").Trim();
            return decimal.TryParse(text, NumberStyles.Number, Inv, out amount);
        }

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
            string icon;
            if (kind == "warn")
                icon = "circle-alert";
            else
                icon = "circle-check";
            litNotice.Text = "<div class=\"notice notice-" + kind + "\">" + Icons.Get(icon, "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(text) + "</span></div>";
        }

        private static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2", Inv);
        }

        protected static string DigitsOf(string contact)
        {
            return new string((contact ?? "").Where(char.IsDigit).ToArray());
        }

        protected static string SuggestNote(Customer c)
        {
            if (c.JobCount == 0)
                return "no orders yet";
            else
                return c.JobCount + (c.JobCount == 1 ? " order" : " orders");
        }

        protected static string PriceData(decimal price)
        {
            return price.ToString("0.00", Inv);
        }

        protected bool IsTicked(int addOnId)
        {
            return IsPostBack && (Request.Form.GetValues("addon") ?? new string[0]).Contains(addOnId.ToString(Inv));
        }

        protected string QuantityOf(int addOnId)
        {
            int qty;
            return IsPostBack && int.TryParse(Request.Form["qty_" + addOnId], NumberStyles.None, Inv, out qty)
                ? qty.ToString(Inv) : "0";
        }
    }
}

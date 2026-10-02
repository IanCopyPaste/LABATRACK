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

namespace LABATRACK.admin
{
    public partial class EditJob : AdminBasePage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected JobOrder Job { get; private set; }
        protected decimal AmountPaid { get; private set; }
        protected PriceQuote Quote { get; private set; }
        protected decimal Difference { get; private set; }
        protected string SettleState { get; private set; }
        protected string RatesData { get; private set; }
        protected string MaxWeightData { get; private set; }

        protected bool ServiceChangeAllowed
        {
            get { return Job.CurrentStatus == JobStatus.Queued || Job.CurrentStatus == JobStatus.IntakeInspection; }
        }

        private List<JobAddOn> currentLines = new List<JobAddOn>();
        private List<LaundryService> services = new List<LaundryService>();
        private List<AddOn> addOns = new List<AddOn>();
        private ShopSettings settings = new ShopSettings();

        protected void Page_Load(object sender, EventArgs e)
        {
            int id;
            if (int.TryParse(Request.QueryString["id"], out id))
                Job = ReadJob(id);

            if (Job == null)
            {
                Response.Redirect("Dashboard.aspx", true);
                return;
            }
            if (!Job.IsInShop)
            {
                Response.Redirect("../pages/shared/JobDetail.aspx?id=" + Job.JobId, true);
                return;
            }

            currentLines = ReadLines(Job.JobId);
            AmountPaid = ReadAmountPaid(Job.JobId);
            LoadSettings();
            MaxWeightData = settings.MaxWeightKg.ToString("0.##", Inv);

            LoadServicesAndAddOns();
            services = services.Where(s => s.IsActive || s.ServiceId == Job.ServiceId).ToList();
            addOns = addOns.Where(a => a.IsActive || currentLines.Any(l => l.AddOnId == a.AddOnId)).ToList();
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
            hfShownStatus.Value = Job.CurrentStatus;
            if (Quote != null)
                hfShownDifference.Value = Money(Difference);
            else
                hfShownDifference.Value = "";
        }

        private void Save()
        {
            string nameProblem = null;
            string contactProblem = null;
            string emailProblem = null;
            CheckCustomer(txtCustomerName.Text, txtContactNumber.Text, txtEmail.Text, Job.CustomerId, out nameProblem, out contactProblem, out emailProblem);
            if (nameProblem != null || contactProblem != null || emailProblem != null)
            {
                if (nameProblem != null) ShowFieldError(lblNameError, nameProblem);
                if (contactProblem != null) ShowFieldError(lblContactError, contactProblem);
                if (emailProblem != null) ShowFieldError(lblEmailError, emailProblem);
                ShowNotice("warn", "Nothing was saved. Check the customer's details.");
                return;
            }

            decimal weight;
            if (!TryWeight(out weight))
            {
                ShowNotice("warn", "Nothing was saved. Enter the weight in kilos, for example 5.1.");
                return;
            }

            decimal? shownDifference = null;
            decimal shown;
            if (decimal.TryParse(hfShownDifference.Value, NumberStyles.Number, Inv, out shown))
                shownDifference = shown;

            string method;
            if (rbAdjCash.Checked)
                method = Payment.Cash;
            else
                method = Payment.Ewallet;
            int serviceId = SelectedServiceId();
            string expectedStatus = hfShownStatus.Value;
            Dictionary<int, int> quantities = PostedQuantities();
            int ownerUserId = CurrentUser.UserId;

            string error = null;
            EditSummary summary = new EditSummary();

            if (weight <= 0)
                error = "Enter the weight in kilos, for example 5.1.";
            else if (weight != Math.Round(weight, 2))
                error = "Enter the weight with at most two decimals, for example 5.25.";
            else if (weight > settings.MaxWeightKg)
                error = "One job can be at most " + settings.MaxWeightKg.ToString("0.##") + " kg. Split a bigger load into two or more jobs.";

            string remarks = (txtRemarks.Text ?? "").Trim();
            if (error == null && remarks.Length > 500)
                error = "Keep the remarks to 500 characters.";
            if (remarks == "")
                remarks = null;

            if (error == null)
            {
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        JobOrder job = null;
                        using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetForUpdate", conn, tx))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = Job.JobId;

                            using (SqlDataReader r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                    job = JobOrder.FromRow(r);
                            }
                        }

                        bool serviceChanged = job != null && serviceId != job.ServiceId;
                        List<JobAddOn> linesNow = new List<JobAddOn>();
                        PriceQuote quote = null;
                        decimal difference = 0m;

                        if (job == null)
                        {
                            error = "That job could not be found.";
                        }
                        else if (job.CurrentStatus != expectedStatus)
                        {
                            error = JobStatus.AlreadyUpdated;
                        }
                        else if (!job.IsInShop)
                        {
                            error = "A " + job.CurrentStatus.ToLowerInvariant() + " job cannot be changed.";
                        }
                        else if (serviceChanged && job.CurrentStatus != JobStatus.Queued && job.CurrentStatus != JobStatus.IntakeInspection)
                        {
                            error = "The service can only be changed while the job is still Queued or in Intake inspection.";
                        }
                        else
                        {
                            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_GetByJob", conn, tx))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = job.JobId;

                                using (SqlDataReader r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        JobAddOn line = new JobAddOn();
                                        line.JobAddOnId = (int)r["JobAddOnId"];
                                        line.JobId = (int)r["JobId"];
                                        line.AddOnId = (int)r["AddOnId"];
                                        line.Name = (string)r["Name"];
                                        line.Kind = (string)r["Kind"];
                                        line.Quantity = (int)r["Quantity"];
                                        line.PriceAtCreation = (decimal)r["PriceAtCreation"];
                                        linesNow.Add(line);
                                    }
                                }
                            }

                            error = BuildQuote(job, linesNow, serviceId, weight, quantities, out quote);
                        }

                        if (error == null)
                        {
                            decimal paidNow = 0m;
                            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetJobTotals", conn, tx))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = job.JobId;

                                using (SqlDataReader r = cmd.ExecuteReader())
                                {
                                    if (r.Read())
                                        paidNow = (decimal)r["AmountPaid"];
                                }
                            }

                            difference = quote.Total - paidNow;
                            if (shownDifference != difference)
                                error = "After this correction the job would be " + SettleText(difference)
                                      + ". Check it with the customer, then save again.";
                            else if (difference < 0 && method != Payment.Cash && method != Payment.Ewallet)
                                error = "Choose how the excess is refunded.";
                        }

                        if (error == null)
                        {
                            string newName = txtCustomerName.Text.Trim();
                            string newContact = txtContactNumber.Text.Trim();
                            string newEmail = (txtEmail.Text ?? "").Trim();
                            string oldEmail = Job.CustomerEmail ?? "";

                            if (newName != Job.CustomerName || newContact != Job.ContactNumber || newEmail != oldEmail)
                            {
                                try
                                {
                                    using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Update", conn, tx))
                                    {
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = job.CustomerId;
                                        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = newName;
                                        cmd.Parameters.Add("@ContactNumber", SqlDbType.NVarChar, 20).Value = newContact;
                                        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = newEmail == "" ? (object)DBNull.Value : newEmail;

                                        cmd.ExecuteScalar();
                                    }
                                }
                                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                                {
                                    error = "This number was just added for another customer.";
                                }
                            }

                            if (newName != Job.CustomerName)
                                summary.Changes.Add("Name: " + Job.CustomerName + " → " + newName);
                            if (newContact != Job.ContactNumber)
                                summary.Changes.Add("Contact number: " + Job.ContactNumber + " → " + newContact);
                            if (newEmail != oldEmail)
                                summary.Changes.Add("Email address updated");
                        }

                        if (error == null)
                        {
                            LaundryService newService = serviceChanged ? services.First(s => s.ServiceId == serviceId) : null;
                            if (serviceChanged)
                                summary.Changes.Add("Service: " + job.ServiceName + " → " + newService.Name);
                            if (weight != job.WeightKg)
                                summary.Changes.Add("Weight: " + job.WeightKg.ToString("0.0#", Inv) + " kg → " + weight.ToString("0.0#", Inv) + " kg");

                            bool washing;
                            if (newService != null)
                                washing = newService.IncludesWashing;
                            else
                                washing = job.IncludesWashing;
                            bool drying;
                            if (newService != null)
                                drying = newService.IncludesDrying;
                            else
                                drying = job.IncludesDrying;
                            bool folding;
                            if (newService != null)
                                folding = newService.IncludesFolding;
                            else
                                folding = job.IncludesFolding;
                            decimal oldTotal = job.TotalAmount;

                            int rowsAffected;
                            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_Update", conn, tx))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = job.JobId;
                                cmd.Parameters.Add("@ExpectedStatus", SqlDbType.NVarChar, 20).Value = expectedStatus;
                                cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = serviceId;
                                cmd.Parameters.Add(new SqlParameter("@WeightKg", SqlDbType.Decimal) { Precision = 6, Scale = 2, Value = weight });
                                cmd.Parameters.Add(new SqlParameter("@RatePerKgAtCreation", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = quote.RatePerKg });
                                cmd.Parameters.Add(new SqlParameter("@LaundryCharge", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = quote.LaundryCharge });
                                cmd.Parameters.Add("@IncludesWashing", SqlDbType.Bit).Value = washing;
                                cmd.Parameters.Add("@IncludesDrying", SqlDbType.Bit).Value = drying;
                                cmd.Parameters.Add("@IncludesFolding", SqlDbType.Bit).Value = folding;
                                cmd.Parameters.Add(new SqlParameter("@TotalAmount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = quote.Total });
                                cmd.Parameters.Add("@Remarks", SqlDbType.NVarChar, 500).Value = remarks == null ? (object)DBNull.Value : remarks;
                                cmd.Parameters.Add("@EditedByUserId", SqlDbType.Int).Value = ownerUserId;

                                rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            if (rowsAffected != 1)
                                error = JobStatus.AlreadyUpdated;
                            else
                            {
                                foreach (JobAddOn line in linesNow)
                                {
                                    PriceLine wanted = quote.Lines.FirstOrDefault(l => l.AddOnId == line.AddOnId);
                                    if (wanted == null)
                                    {
                                        using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_Remove", conn, tx))
                                        {
                                            cmd.CommandType = CommandType.StoredProcedure;
                                            cmd.Parameters.Add("@JobAddOnId", SqlDbType.Int).Value = line.JobAddOnId;

                                            cmd.ExecuteNonQuery();
                                        }
                                        summary.Changes.Add("Removed: " + line.Name);
                                    }
                                    else if (wanted.Quantity != line.Quantity)
                                    {
                                        using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_SetQuantity", conn, tx))
                                        {
                                            cmd.CommandType = CommandType.StoredProcedure;
                                            cmd.Parameters.Add("@JobAddOnId", SqlDbType.Int).Value = line.JobAddOnId;
                                            cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = wanted.Quantity;

                                            cmd.ExecuteNonQuery();
                                        }
                                        summary.Changes.Add(line.Name + ": × " + line.Quantity + " → × " + wanted.Quantity);
                                    }
                                }

                                foreach (PriceLine line in quote.Lines.Where(l => linesNow.All(c => c.AddOnId != l.AddOnId)))
                                {
                                    using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_Insert", conn, tx))
                                    {
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = job.JobId;
                                        cmd.Parameters.Add("@AddOnId", SqlDbType.Int).Value = line.AddOnId;
                                        cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = line.Quantity;
                                        cmd.Parameters.Add(new SqlParameter("@PriceAtCreation", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = line.UnitPrice });

                                        cmd.ExecuteNonQuery();
                                    }
                                    summary.Changes.Add("Added: " + line.Name + (line.IsProduct ? " × " + line.Quantity : ""));
                                }

                                if (quote.Total != oldTotal)
                                    summary.Changes.Add("Total: " + Peso(oldTotal) + " → " + Peso(quote.Total));

                                if (difference < 0)
                                {
                                    using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_Insert", conn, tx))
                                    {
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = job.JobId;
                                        cmd.Parameters.Add("@Kind", SqlDbType.NVarChar, 12).Value = Payment.Adjustment;
                                        cmd.Parameters.Add(new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = difference });
                                        cmd.Parameters.Add("@Method", SqlDbType.NVarChar, 10).Value = method;
                                        cmd.Parameters.Add(new SqlParameter("@CashReceived", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = DBNull.Value });
                                        cmd.Parameters.Add("@RecordedByUserId", SqlDbType.Int).Value = ownerUserId;

                                        cmd.ExecuteNonQuery();
                                    }
                                    summary.RefundAmount = -difference;
                                    summary.RefundMethod = method;
                                }
                            }
                        }

                        if (error == null)
                            tx.Commit();
                    }
                }
            }

            if (error == null)
            {
                if (summary.Changes.Count > 0)
                    Email.SendModified(Job.JobId, ownerUserId, summary);

                Response.Redirect("../pages/shared/JobDetail.aspx?id=" + Job.JobId + "&done=edited", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            ShowNotice("warn", "Nothing was saved. " + error);
            Job = ReadJob(Job.JobId);
            AmountPaid = ReadAmountPaid(Job.JobId);
            ddlService.Enabled = ServiceChangeAllowed;
        }

        private string BuildQuote(JobOrder job, List<JobAddOn> lines, int serviceId, decimal weight, Dictionary<int, int> quantities, out PriceQuote quote)
        {
            quote = null;
            decimal rate = job.RatePerKgAtCreation;
            if (serviceId != job.ServiceId)
            {
                LaundryService chosen = services.FirstOrDefault(s => s.ServiceId == serviceId);
                if (chosen == null || !chosen.IsActive)
                    return "Choose a service that is still offered.";
                rate = chosen.RatePerKg;
            }

            foreach (int id in quantities.Keys)
            {
                if (addOns.All(a => a.AddOnId != id))
                    return "One of the add-ons is not on the price list any more. Reload the page.";
            }

            PriceQuote q = new PriceQuote();
            q.WeightKg = weight;
            q.RatePerKg = rate;
            q.MinimumCharge = job.MinimumChargeAtCreation;
            q.ChargeBeforeMinimum = Math.Round(weight * rate, 2, MidpointRounding.AwayFromZero);
            if (q.ChargeBeforeMinimum < job.MinimumChargeAtCreation)
                q.LaundryCharge = job.MinimumChargeAtCreation;
            else
                q.LaundryCharge = q.ChargeBeforeMinimum;

            decimal total = q.LaundryCharge;
            foreach (AddOn a in addOns)
            {
                int qty;
                if (!quantities.TryGetValue(a.AddOnId, out qty) || qty <= 0) continue;

                JobAddOn onJob = lines.FirstOrDefault(l => l.AddOnId == a.AddOnId);
                if (onJob == null && !a.IsActive)
                    return a.Name + " is no longer offered, so it cannot be added.";

                PriceLine line = new PriceLine();
                line.AddOnId = a.AddOnId;
                line.Name = a.Name;
                line.IsProduct = a.IsProduct;
                line.Quantity = qty;
                if (onJob != null)
                    line.UnitPrice = onJob.PriceAtCreation;
                else
                    line.UnitPrice = a.Price;
                line.Amount = Math.Round(qty * line.UnitPrice, 2, MidpointRounding.AwayFromZero);

                if (!line.IsProduct && line.Quantity != 1)
                    return line.Name + " is charged once per job.";
                if (line.IsProduct && line.Quantity > 99)
                    return "Enter at most 99 of " + line.Name + ".";

                q.Lines.Add(line);
                total += line.Amount;
            }
            q.Total = total;

            quote = q;
            return null;
        }

        private void ShowQuote()
        {
            Quote = null;
            decimal weight;
            if (TryWeight(out weight) && weight > 0)
            {
                PriceQuote quote;
                string problem = BuildQuote(Job, currentLines, SelectedServiceId(), weight, CurrentQuantities(), out quote);
                if (problem == null)
                    Quote = quote;
            }
            if (Quote != null)
                Difference = Quote.Total - AmountPaid;
            else
                Difference = 0m;
            SettleState = Difference > 0 ? "balance" : Difference < 0 ? "refund" : "none";
        }

        private void CheckCustomer(string name, string contact, string email, int editingCustomerId,
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

            string digits = new string(contact.Where(char.IsDigit).ToArray());
            if (contact == "")
            {
                contactProblem = "Enter a contact number. It is how the customer is found next time.";
            }
            else if (contact.Length > 20 || digits.Length < 7 || digits.Length > 15
                     || contact.Any(ch => !char.IsDigit(ch) && " +-()".IndexOf(ch) < 0))
            {
                contactProblem = "Use digits only, for example 0917 555 0123.";
            }
            else
            {
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_GetByContactDigits", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ContactDigits", SqlDbType.NVarChar, 20).Value = digits;

                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read() && (int)r["CustomerId"] != editingCustomerId)
                            contactProblem = "This number already belongs to " + (string)r["Name"] + ". Search for it instead of adding a new customer.";
                    }
                }
            }

            if (email != "" && !Email.IsValidAddress(email))
                emailProblem = "That does not look like an email address. Leave it blank if they have none.";
        }

        private static string SettleText(decimal difference)
        {
            if (difference > 0) return Peso(difference) + " short (the balance, paid before pick-up)";
            if (difference < 0) return "overpaid by " + Peso(-difference) + " (refunded now)";
            return "paid in full";
        }

        private JobOrder ReadJob(int id)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = id;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return JobOrder.FromRow(r);
                    return null;
                }
            }
        }

        private List<JobAddOn> ReadLines(int id)
        {
            List<JobAddOn> lines = new List<JobAddOn>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_GetByJob", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = id;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        JobAddOn line = new JobAddOn();
                        line.JobAddOnId = (int)r["JobAddOnId"];
                        line.JobId = (int)r["JobId"];
                        line.AddOnId = (int)r["AddOnId"];
                        line.Name = (string)r["Name"];
                        line.Kind = (string)r["Kind"];
                        line.Quantity = (int)r["Quantity"];
                        line.PriceAtCreation = (decimal)r["PriceAtCreation"];
                        lines.Add(line);
                    }
                }
            }
            return lines;
        }

        private decimal ReadAmountPaid(int id)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetJobTotals", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = id;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return (decimal)r["AmountPaid"];
                    return 0m;
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
                        string value = r["SettingValue"] as string;
                        decimal money;
                        if ((string)r["SettingKey"] == ShopSettings.MaxWeightKgKey && value != null
                            && decimal.TryParse(value, NumberStyles.Number, Inv, out money))
                            settings.MaxWeightKg = money;
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

        private int SelectedServiceId()
        {
            int id;
            if (ServiceChangeAllowed && int.TryParse(ddlService.SelectedValue, out id))
                return id;
            else
                return Job.ServiceId;
        }

        private bool TryWeight(out decimal weight)
        {
            return decimal.TryParse(txtWeight.Text.Trim().Replace(',', '.'), NumberStyles.Number, Inv, out weight);
        }

        private Dictionary<int, int> CurrentQuantities()
        {
            if (IsPostBack)
                return PostedQuantities();
            else
                return currentLines.ToDictionary(l => l.AddOnId, l => l.Quantity);
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

        private decimal RateFor(LaundryService s)
        {
            if (s.ServiceId == Job.ServiceId)
                return Job.RatePerKgAtCreation;
            else
                return s.RatePerKg;
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

        private static void ShowFieldError(Label label, string text)
        {
            label.Text = HttpUtility.HtmlEncode(text);
            label.Visible = true;
        }

        protected string SaveLabel
        {
            get
            {
                if (SettleState == "refund")
                    return "Save and refund " + Peso(-Difference);
                else
                    return "Save changes";
            }
        }

        protected string SettlePill
        {
            get { return SettleState == "refund" ? "pill-danger" : SettleState == "balance" ? "pill-warn" : "pill-ok"; }
        }

        protected string SettlePillText
        {
            get { return SettleState == "refund" ? "Overpaid" : SettleState == "balance" ? "Balance due" : "Paid in full"; }
        }

        protected string BalanceText
        {
            get
            {
                if (Quote == null || Difference <= 0) return "";
                return "The customer has paid " + Peso(AmountPaid) + " of the " + Peso(Quote.Total)
                     + " total. The " + Peso(Difference) + " balance is collected before pick-up, on the job page. Nothing is collected now.";
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

        protected decimal PriceFor(AddOn a)
        {
            JobAddOn line = currentLines.FirstOrDefault(l => l.AddOnId == a.AddOnId);
            if (line != null)
                return line.PriceAtCreation;
            else
                return a.Price;
        }

        protected bool IsTicked(int addOnId)
        {
            int qty;
            return CurrentQuantities().TryGetValue(addOnId, out qty) && qty > 0;
        }

        protected string QuantityOf(int addOnId)
        {
            int qty;
            if (CurrentQuantities().TryGetValue(addOnId, out qty))
                return qty.ToString(Inv);
            else
                return "0";
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
                case "Intake inspection": return "st-intake";
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

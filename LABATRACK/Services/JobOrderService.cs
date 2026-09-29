using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using LABATRACK.DataAccess;
using LABATRACK.Models;

namespace LABATRACK.Services
{
    /// <summary>
    /// Creating a job at the counter, and the owner's corrections to a job. Both follow the
    /// payment rules of the shop:
    ///   - Full payment only, at drop-off. A job is saved only together with one payment for
    ///     exactly its total; there is no partial payment and no balance.
    ///   - Cash: the cash received must be at least the total. Change = cash received - total,
    ///     worked out here, never taken from the browser. E-wallet is the exact total.
    ///   - An owner's correction that changes the total is settled in the same save: a positive
    ///     adjustment when the new total is higher (collected), a negative one when it is lower
    ///     (refunded). So the payments on a job always add up to exactly its total.
    /// Every save here is one SqlTransaction: all of it is saved, or none of it.
    /// </summary>
    public static class JobOrderService
    {
        public const int MaxProductQuantity = 99;

        /// <summary>What the counter entered on the New job page.</summary>
        public class NewJobRequest
        {
            public NewJobRequest()
            {
                AddOnQuantities = new Dictionary<int, int>();
            }

            public int? CustomerId { get; set; }          // set when an existing customer was found
            public string CustomerName { get; set; }
            public string ContactNumber { get; set; }
            public string CustomerEmail { get; set; }
            public int ServiceId { get; set; }
            public decimal WeightKg { get; set; }
            public Dictionary<int, int> AddOnQuantities { get; private set; }   // AddOnId -> how many
            public string Remarks { get; set; }
            public DateTime? ExpectedPickupAt { get; set; }
            public string Method { get; set; }            // Cash or E-wallet
            public decimal? CashReceived { get; set; }
        }

        /// <summary>What the owner entered on the Edit job page.</summary>
        public class EditRequest
        {
            public EditRequest()
            {
                AddOnQuantities = new Dictionary<int, int>();
            }

            public int JobId { get; set; }
            public string ExpectedStatus { get; set; }    // the status the owner saw when the page opened
            public string CustomerName { get; set; }
            public string ContactNumber { get; set; }
            public string CustomerEmail { get; set; }
            public int ServiceId { get; set; }
            public decimal WeightKg { get; set; }
            public Dictionary<int, int> AddOnQuantities { get; private set; }
            public string Remarks { get; set; }
            public string Method { get; set; }            // how the difference is collected or refunded
            public decimal? CashReceived { get; set; }    // cash handed over, when collecting in cash
            public decimal? ShownDifference { get; set; } // the amount to settle the page showed
        }

        // ------------------------------------------------------------ New job

        /// <summary>
        /// Prices a new job with today's prices: the service's rate and the add-ons' prices from the
        /// Pricing page, and the minimum charge and rounding step from Settings.
        /// </summary>
        public static PriceQuote QuoteNew(LaundryService service, decimal weightKg, IDictionary<int, int> quantities,
                                          IList<AddOn> addOns, ShopSettings settings)
        {
            var lines = new List<PriceLine>();
            foreach (AddOn a in addOns)   // in the Pricing page's order: service add-ons first, then products
            {
                int qty;
                if (!quantities.TryGetValue(a.AddOnId, out qty) || qty <= 0) continue;
                if (!a.IsActive)
                    throw new BusinessRuleException(a.Name + " is no longer offered. Untick it and try again.");
                lines.Add(new PriceLine { AddOnId = a.AddOnId, Name = a.Name, IsProduct = a.IsProduct, Quantity = qty, UnitPrice = a.Price });
            }
            CheckQuantities(lines, quantities, addOns);
            return PricingService.Quote(weightKg, service.RatePerKg, settings.MinimumCharge, settings.RoundingIncrementKg, lines);
        }

        /// <summary>
        /// Saves a new job: the customer (if new), the job with its prices and route copied in, its
        /// add-ons, the first history row (Queued) and its one full payment, in one transaction.
        /// Then emails the customer the claim number (a failed email does not undo anything).
        /// Returns the new JobId. Throws BusinessRuleException, and saves nothing, if a rule says no.
        /// </summary>
        public static int Create(NewJobRequest req, int userId)
        {
            ShopSettings settings = SettingsRepository.Get();

            LaundryService service = ServiceRepository.GetById(req.ServiceId);
            if (service == null || !service.IsActive)
                throw new BusinessRuleException("Choose a service.");

            PricingService.CheckWeight(req.WeightKg, settings.MaxWeightKg);
            PriceQuote quote = QuoteNew(service, req.WeightKg, req.AddOnQuantities, AddOnRepository.GetAll(), settings);

            // Full payment now: cash of at least the total, or e-wallet for exactly the total.
            decimal? cashReceived = CheckPayment(req.Method, req.CashReceived, quote.Total, "the total");

            // The customer: the one found by contact number, or a new one. A number that already
            // belongs to someone is never added twice.
            Customer existing = null;
            if (req.CustomerId.HasValue)
            {
                existing = CustomerRepository.GetById(req.CustomerId.Value);
                if (existing == null || CustomerService.Digits(existing.ContactNumber) != CustomerService.Digits(req.ContactNumber))
                    throw new BusinessRuleException("The contact number was changed after the customer was found. Press Find again.");
            }
            else
            {
                Customer sameNumber = CustomerService.FindByContact(req.ContactNumber);
                if (sameNumber != null)
                    throw new BusinessRuleException("This number already belongs to " + sameNumber.Name + ". Press Find to use their record.");
                CustomerService.Problems problems = CustomerService.Check(req.CustomerName, req.ContactNumber, req.CustomerEmail, null);
                if (problems.Any) throw new BusinessRuleException(problems.First);
            }

            // Every load runs on the same turnaround (no rush fee); staff may set a sooner time.
            DateTime now = DateTime.Now;
            DateTime pickup = req.ExpectedPickupAt ?? now.AddHours(settings.DefaultTurnaroundHours);
            if (pickup <= now)
                throw new BusinessRuleException("The expected pick-up must be later than now.");

            string remarks = CleanRemarks(req.Remarks);

            int jobId;
            using (var conn = Db.Open())
            using (var tx = conn.BeginTransaction())
            {
                int customerId = existing != null
                    ? existing.CustomerId
                    : InsertCustomer(conn, tx, req.CustomerName, req.ContactNumber, req.CustomerEmail);

                var job = new JobOrder
                {
                    ClaimNumber = ClaimNumberGenerator.Next(conn, tx, now),
                    CustomerId = customerId,
                    ServiceId = service.ServiceId,
                    WeightKg = req.WeightKg,
                    BillableKg = quote.BillableKg,
                    // Frozen at creation: a later change on the Pricing page never alters this job.
                    RatePerKgAtCreation = service.RatePerKg,
                    MinimumChargeAtCreation = settings.MinimumCharge,
                    RoundingIncrementKgAtCreation = settings.RoundingIncrementKg,
                    LaundryCharge = quote.LaundryCharge,
                    // The route, frozen the same way.
                    IncludesWashing = service.IncludesWashing,
                    IncludesDrying = service.IncludesDrying,
                    IncludesFolding = service.IncludesFolding,
                    TotalAmount = quote.Total,
                    CurrentStatus = StatusService.Queued,
                    Remarks = remarks,
                    ExpectedPickupAt = pickup,
                    CreatedByUserId = userId,
                };
                jobId = JobOrderRepository.Insert(conn, tx, job);

                foreach (PriceLine line in quote.Lines)
                    JobOrderRepository.InsertAddOn(conn, tx, jobId, line.AddOnId, line.Quantity, line.UnitPrice);

                StatusHistoryRepository.Insert(conn, tx, jobId, null, StatusService.Queued, userId, null);
                PaymentRepository.Insert(conn, tx, jobId, Payment.DropOff, quote.Total, req.Method, cashReceived, userId);

                tx.Commit();
            }

            EmailService.SendDropOff(jobId, userId, false);
            return jobId;
        }

        // ------------------------------------------------------------ Owner's correction

        /// <summary>
        /// Prices a corrected job. The prices frozen on the job are used: its rate per kg, its
        /// minimum charge and rounding step, and each existing add-on line's price. Only something
        /// newly chosen in the edit (a different service, or an add-on not on the job before) takes
        /// today's price, which is then frozen onto the job like any other.
        /// </summary>
        public static PriceQuote QuoteEdit(JobOrder job, IList<JobAddOn> currentLines, int serviceId, decimal weightKg,
                                           IDictionary<int, int> quantities, IList<AddOn> addOns, IList<LaundryService> services)
        {
            decimal rate = job.RatePerKgAtCreation;
            if (serviceId != job.ServiceId)
            {
                LaundryService chosen = services.FirstOrDefault(s => s.ServiceId == serviceId);
                if (chosen == null || !chosen.IsActive)
                    throw new BusinessRuleException("Choose a service that is still offered.");
                rate = chosen.RatePerKg;
            }

            var lines = new List<PriceLine>();
            foreach (AddOn a in addOns)
            {
                int qty;
                if (!quantities.TryGetValue(a.AddOnId, out qty) || qty <= 0) continue;
                JobAddOn onJob = currentLines.FirstOrDefault(l => l.AddOnId == a.AddOnId);
                if (onJob == null && !a.IsActive)
                    throw new BusinessRuleException(a.Name + " is no longer offered, so it cannot be added.");
                lines.Add(new PriceLine
                {
                    AddOnId = a.AddOnId,
                    Name = a.Name,
                    IsProduct = a.IsProduct,
                    Quantity = qty,
                    UnitPrice = onJob != null ? onJob.PriceAtCreation : a.Price,
                });
            }
            CheckQuantities(lines, quantities, addOns);
            return PricingService.Quote(weightKg, rate, job.MinimumChargeAtCreation, job.RoundingIncrementKgAtCreation, lines);
        }

        /// <summary>
        /// Saves the owner's correction of a job that is not yet Claimed or Voided. The service can
        /// change only while the job is still Queued (another service can have another route);
        /// weight, add-ons, remarks and the customer's details can be corrected until it is claimed.
        /// The difference in the total is settled in the same transaction. Throws
        /// BusinessRuleException, and saves nothing, if a rule says no.
        /// </summary>
        public static void SaveEdit(EditRequest req, int ownerUserId)
        {
            ShopSettings settings = SettingsRepository.Get();
            List<AddOn> addOns = AddOnRepository.GetAll();
            List<LaundryService> services = ServiceRepository.GetAll();

            JobOrder before = JobOrderRepository.GetById(req.JobId);
            if (before == null)
                throw new BusinessRuleException("That job could not be found.");

            // The customer's details are checked before the transaction starts.
            CustomerService.Problems problems = CustomerService.Check(req.CustomerName, req.ContactNumber, req.CustomerEmail, before.CustomerId);
            if (problems.Any) throw new BusinessRuleException(problems.First);
            PricingService.CheckWeight(req.WeightKg, settings.MaxWeightKg);
            string remarks = CleanRemarks(req.Remarks);

            using (var conn = Db.Open())
            using (var tx = conn.BeginTransaction())
            {
                // Lock the job, then check it is still where the owner saw it and still editable.
                JobOrder job = JobOrderRepository.GetForUpdate(conn, tx, req.JobId);
                if (job.CurrentStatus != req.ExpectedStatus)
                    throw new BusinessRuleException(StatusService.AlreadyUpdated);
                if (!StatusService.IsInShop(job.CurrentStatus))
                    throw new BusinessRuleException("A " + job.CurrentStatus.ToLowerInvariant() + " job cannot be changed.");
                bool serviceChanged = req.ServiceId != job.ServiceId;
                if (serviceChanged && job.CurrentStatus != StatusService.Queued)
                    throw new BusinessRuleException("The service can only be changed while the job is still Queued.");

                List<JobAddOn> currentLines = JobOrderRepository.GetAddOns(conn, tx, job.JobId);
                PriceQuote quote = QuoteEdit(job, currentLines, req.ServiceId, req.WeightKg, req.AddOnQuantities, addOns, services);

                // The difference to settle: the new total minus what has been paid (vw_JobPayments).
                JobPayments paid = PaymentRepository.GetJobTotals(conn, tx, job.JobId);
                decimal difference = quote.Total - paid.AmountPaid;
                if (req.ShownDifference != difference)
                    throw new BusinessRuleException("The amount to settle is now " + SettleText(difference)
                                                    + ". Check it with the customer, then save again.");
                decimal? cashReceived = difference > 0
                    ? CheckPayment(req.Method, req.CashReceived, difference, "the amount to collect")
                    : null;
                if (difference < 0 && req.Method != Payment.Cash && req.Method != Payment.Ewallet)
                    throw new BusinessRuleException("Choose how the difference is refunded.");

                // 1. The customer's details, when they changed.
                if (req.CustomerName.Trim() != before.CustomerName || req.ContactNumber.Trim() != before.ContactNumber
                    || (req.CustomerEmail ?? "").Trim() != (before.CustomerEmail ?? ""))
                    UpdateCustomer(conn, tx, job.CustomerId, req.CustomerName, req.ContactNumber, req.CustomerEmail);

                // 2. The job itself, guarded by the status the owner saw.
                LaundryService newService = serviceChanged ? services.First(s => s.ServiceId == req.ServiceId) : null;
                job.ServiceId = req.ServiceId;
                job.WeightKg = req.WeightKg;
                job.BillableKg = quote.BillableKg;
                job.RatePerKgAtCreation = quote.RatePerKg;
                job.LaundryCharge = quote.LaundryCharge;
                if (newService != null)
                {
                    job.IncludesWashing = newService.IncludesWashing;
                    job.IncludesDrying = newService.IncludesDrying;
                    job.IncludesFolding = newService.IncludesFolding;
                }
                job.TotalAmount = quote.Total;
                job.Remarks = remarks;
                if (!JobOrderRepository.Update(conn, tx, job, req.ExpectedStatus, ownerUserId))
                    throw new BusinessRuleException(StatusService.AlreadyUpdated);

                // 3. The add-on lines: a changed quantity keeps its frozen price; a line taken off is
                //    marked removed (never deleted); a new one is added at today's price.
                foreach (JobAddOn line in currentLines)
                {
                    PriceLine wanted = quote.Lines.FirstOrDefault(l => l.AddOnId == line.AddOnId);
                    if (wanted == null)
                        JobOrderRepository.RemoveAddOn(conn, tx, line.JobAddOnId);
                    else if (wanted.Quantity != line.Quantity)
                        JobOrderRepository.SetAddOnQuantity(conn, tx, line.JobAddOnId, wanted.Quantity);
                }
                foreach (PriceLine line in quote.Lines.Where(l => currentLines.All(c => c.AddOnId != l.AddOnId)))
                    JobOrderRepository.InsertAddOn(conn, tx, job.JobId, line.AddOnId, line.Quantity, line.UnitPrice);

                // 4. Settle the difference: collected (+) or refunded (-). No row when it is the same.
                if (difference != 0)
                    PaymentRepository.Insert(conn, tx, job.JobId, Payment.Adjustment, difference, req.Method, cashReceived, ownerUserId);

                tx.Commit();
            }
        }

        /// <summary>"collect ₱35.00", "refund ₱55.00" or "nothing (the total is the same)".</summary>
        public static string SettleText(decimal difference)
        {
            if (difference > 0) return "collect ₱" + difference.ToString("N2", CultureInfo.InvariantCulture);
            if (difference < 0) return "refund ₱" + (-difference).ToString("N2", CultureInfo.InvariantCulture);
            return "nothing (the total is the same)";
        }

        // ------------------------------------------------------------ Pieces

        /// <summary>
        /// The payment rule for money coming in. Cash must cover the amount (the change is
        /// cash received - amount); e-wallet is recorded as exactly the amount, with no cash.
        /// Returns the cash received to store, or null for e-wallet.
        /// </summary>
        private static decimal? CheckPayment(string method, decimal? cashReceived, decimal amount, string amountName)
        {
            if (method == Payment.Ewallet)
                return null;
            if (method != Payment.Cash)
                throw new BusinessRuleException("Choose how the customer paid: Cash or E-wallet.");
            if (!cashReceived.HasValue || cashReceived.Value < amount)
                throw new BusinessRuleException("Cash received must be at least " + amountName + " of ₱"
                                                + amount.ToString("N2", CultureInfo.InvariantCulture) + ".");
            if (cashReceived.Value != Math.Round(cashReceived.Value, 2) || cashReceived.Value > 99999999m)
                throw new BusinessRuleException("Enter the cash received in pesos, for example 500 or 500.00.");
            return cashReceived.Value;
        }

        /// <summary>A service add-on is charged once (quantity 1); a product is 1 to 99 pieces.</summary>
        private static void CheckQuantities(IEnumerable<PriceLine> lines, IDictionary<int, int> quantities, IList<AddOn> addOns)
        {
            foreach (int id in quantities.Keys)
                if (addOns.All(a => a.AddOnId != id))
                    throw new BusinessRuleException("One of the add-ons is not on the price list any more. Reload the page.");

            foreach (PriceLine line in lines)
            {
                if (!line.IsProduct && line.Quantity != 1)
                    throw new BusinessRuleException(line.Name + " is charged once per job.");
                if (line.IsProduct && line.Quantity > MaxProductQuantity)
                    throw new BusinessRuleException("Enter at most " + MaxProductQuantity + " of " + line.Name + ".");
            }
        }

        private static int InsertCustomer(SqlConnection conn, SqlTransaction tx, string name, string contact, string email)
        {
            try
            {
                return CustomerRepository.Insert(conn, tx, name.Trim(), contact.Trim(), EmptyToNull(email));
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                // The unique index on the contact number: someone added this number a moment ago.
                throw new BusinessRuleException("This number was just added for another customer. Press Find to use their record.");
            }
        }

        private static void UpdateCustomer(SqlConnection conn, SqlTransaction tx, int customerId, string name, string contact, string email)
        {
            try
            {
                CustomerRepository.Update(conn, tx, customerId, name.Trim(), contact.Trim(), EmptyToNull(email));
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new BusinessRuleException("This number was just added for another customer.");
            }
        }

        private static string CleanRemarks(string remarks)
        {
            remarks = (remarks ?? "").Trim();
            if (remarks.Length > 500)
                throw new BusinessRuleException("Keep the remarks to 500 characters.");
            return remarks == "" ? null : remarks;
        }

        private static string EmptyToNull(string s)
        {
            s = (s ?? "").Trim();
            return s == "" ? null : s;
        }
    }
}

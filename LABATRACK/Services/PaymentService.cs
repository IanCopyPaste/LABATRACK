using System;
using System.Data.SqlClient;
using System.Globalization;
using LABATRACK.DataAccess;
using LABATRACK.Models;

namespace LABATRACK.Services
{
    /// <summary>
    /// The shop's payment rules, in one place:
    ///   - At drop-off the customer pays the full total, or at least the minimum down payment:
    ///     a percentage of the total the owner sets on the Pricing page (0% allows paying
    ///     everything at pick-up; 100% means full payment only).
    ///   - What is still owed (the balance) can be paid any time while the load is in the shop,
    ///     in one or more payments, and must be paid in full before the load is claimed
    ///     (StatusService.Claim collects it in the same save).
    ///   - Money in: cash must cover the amount, and the change is cash received - amount,
    ///     worked out here, never taken from the browser. E-wallet is exactly the amount.
    ///   - A job is Paid, Partially paid or Unpaid by comparing what was paid (vw_JobPayments)
    ///     with its total. It is worked out every time, never stored, so it can never go stale.
    /// </summary>
    public static class PaymentService
    {
        public const string Paid = "Paid";
        public const string PartiallyPaid = "Partially paid";
        public const string Unpaid = "Unpaid";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Paid when nothing is owed, Partially paid when something was paid, else Unpaid.</summary>
        public static string StatusOf(decimal total, decimal paid)
        {
            if (paid >= total) return Paid;
            if (paid > 0) return PartiallyPaid;
            return Unpaid;
        }

        /// <summary>
        /// The least that may be paid at drop-off: percent of the total, rounded up to the
        /// centavo. Example: 50% of ₱135.00 = ₱67.50; 50% of ₱232.00 = ₱116.00.
        /// </summary>
        public static decimal MinimumDownPayment(decimal total, int percent)
        {
            return Math.Ceiling(total * percent) / 100m;
        }

        /// <summary>
        /// Checks what the customer pays at drop-off: from the minimum down payment up to the
        /// whole total. Nothing at all is allowed only when the minimum is 0%.
        /// </summary>
        public static void CheckDropOff(decimal total, decimal amountNow, int percent)
        {
            CheckPesos(amountNow, "Enter the amount paid now in pesos, for example 150 or 150.00.");
            if (amountNow > total)
                throw new BusinessRuleException("The amount paid now can't be more than the " + Peso(total) + " total.");
            decimal minimum = MinimumDownPayment(total, percent);
            if (amountNow < minimum)
                throw new BusinessRuleException("The down payment must be at least " + Peso(minimum)
                                                + " (" + percent + "% of the " + Peso(total) + " total).");
        }

        /// <summary>
        /// The rule for money coming in. Cash must cover the amount (the change is cash received
        /// - amount); e-wallet is recorded as exactly the amount, with no cash. Returns the cash
        /// received to store, or null for e-wallet.
        /// </summary>
        public static decimal? CheckIncoming(string method, decimal? cashReceived, decimal amount)
        {
            if (method == Payment.Ewallet)
                return null;
            if (method != Payment.Cash)
                throw new BusinessRuleException("Choose how the customer paid: Cash or E-wallet.");
            if (!cashReceived.HasValue || cashReceived.Value < amount)
                throw new BusinessRuleException("Cash received must be at least the " + Peso(amount) + " being paid.");
            CheckPesos(cashReceived.Value, "Enter the cash received in pesos, for example 500 or 500.00.");
            return cashReceived.Value;
        }

        /// <summary>
        /// Records a payment towards a job's balance while the load is still in the shop: part of
        /// it, or all of it. <paramref name="shownBalance"/> is the balance the page showed; if it
        /// changed in the meantime (someone else took a payment), nothing is saved, so the same
        /// money is never taken twice.
        /// </summary>
        public static void Collect(int jobId, decimal amount, string method, decimal? cashReceived, decimal shownBalance, int userId)
        {
            CheckPesos(amount, "Enter the amount paid in pesos, for example 100 or 100.00.");
            if (amount <= 0)
                throw new BusinessRuleException("Enter the amount the customer is paying.");

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    // Lock the job, so two payments on it are taken one after the other.
                    JobOrder job = JobOrderRepository.GetForUpdate(conn, tx, jobId);
                    if (job == null)
                        throw new BusinessRuleException("That job could not be found.");
                    if (!StatusService.IsInShop(job.CurrentStatus))
                        throw new BusinessRuleException("A " + job.CurrentStatus.ToLowerInvariant() + " job takes no more payments.");

                    decimal balance = BalanceOf(conn, tx, jobId);
                    if (balance != shownBalance)
                        throw new BusinessRuleException("The balance is now " + Peso(balance) + ". Check it with the customer, then try again.");
                    if (balance <= 0)
                        throw new BusinessRuleException("This job is already paid in full.");
                    if (amount > balance)
                        throw new BusinessRuleException("The balance is only " + Peso(balance) + ". Enter at most that.");

                    decimal? cash = CheckIncoming(method, cashReceived, amount);
                    PaymentRepository.Insert(conn, tx, jobId, Payment.BalancePayment, amount, method, cash, userId);
                    tx.Commit();
                }
            }
        }

        /// <summary>What is still owed on a job, read inside the caller's transaction (vw_JobPayments).</summary>
        public static decimal BalanceOf(SqlConnection conn, SqlTransaction tx, int jobId)
        {
            return PaymentRepository.GetJobTotals(conn, tx, jobId).Balance;
        }

        public static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2", Inv);
        }

        /// <summary>Whole centavos, not negative, and within what the table can hold.</summary>
        private static void CheckPesos(decimal amount, string message)
        {
            if (amount < 0 || amount != Math.Round(amount, 2) || amount > 99999999m)
                throw new BusinessRuleException(message);
        }
    }
}

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using LABATRACK.DataAccess;
using LABATRACK.Models;

namespace LABATRACK.Services
{
    /// <summary>
    /// Moves a job through its stages. It is the only code that changes a job's status, and it
    /// allows only these moves (everything else is refused):
    ///
    ///   Queued            -> the first work stage on the job's route, or Voided (reason required)
    ///   a work stage      -> the next work stage on the route, or Inspection after the last one
    ///   Inspection        -> Ready for pick-up (pass; sends the ready email)
    ///   Inspection        -> a work stage on the route (fail: rework, reason required)
    ///   Ready for pick-up -> Claimed (only with nothing owed: Claim collects the balance first)
    ///   Claimed, Voided   -> nothing: they are final
    ///
    /// Each job has its own route, copied from its service when it was created: Queued, then the
    /// work stages the service includes (always in the order Washing, Drying, Folding), then
    /// Inspection, Ready for pick-up and Claimed. A Wash-Dry job goes from Drying straight to
    /// Inspection, and Folding is never offered for it anywhere.
    ///
    /// Every move is one transaction: lock the job, update CurrentStatus only if it is still
    /// what the person was looking at (else "This job was already updated. Refresh."), and add
    /// one JobStatusHistory row with the time, the account and any reason.
    /// </summary>
    public static class StatusService
    {
        public const string Queued = "Queued";
        public const string Washing = "Washing";
        public const string Drying = "Drying";
        public const string Folding = "Folding";
        public const string Inspection = "Inspection";
        public const string Ready = "Ready for pick-up";
        public const string Claimed = "Claimed";
        public const string Voided = "Voided";

        public const string AlreadyUpdated = "This job was already updated. Refresh.";

        /// <summary>The stages a load is in while it is still in the shop (the board's columns).</summary>
        public static readonly string[] BoardStages = { Queued, Washing, Drying, Folding, Inspection, Ready };

        // ------------------------------------------------------------ The route

        /// <summary>The work stages a route includes, always in the order Washing, Drying, Folding.</summary>
        public static List<string> WorkStages(bool washing, bool drying, bool folding)
        {
            var stages = new List<string>();
            if (washing) stages.Add(Washing);
            if (drying) stages.Add(Drying);
            if (folding) stages.Add(Folding);
            return stages;
        }

        public static List<string> WorkStages(JobOrder job)
        {
            return WorkStages(job.IncludesWashing, job.IncludesDrying, job.IncludesFolding);
        }

        /// <summary>The whole route of a job, from Queued to Claimed.</summary>
        public static List<string> Route(JobOrder job)
        {
            var route = new List<string> { Queued };
            route.AddRange(WorkStages(job));
            route.AddRange(new[] { Inspection, Ready, Claimed });
            return route;
        }

        /// <summary>True while a job is still in the shop (not Claimed and not Voided).</summary>
        public static bool IsInShop(string status)
        {
            return status != Claimed && status != Voided;
        }

        /// <summary>The next step forward on the job's route, or null if the job is Claimed or Voided.</summary>
        public static string NextStage(JobOrder job)
        {
            List<string> route = Route(job);
            int at = route.IndexOf(job.CurrentStatus);
            return at >= 0 && at < route.Count - 1 ? route[at + 1] : null;
        }

        /// <summary>
        /// Where a failed inspection may send the load: the work stages on the job's own route that
        /// the owner allows in Settings (open decision 5). If the setting leaves none of this job's
        /// stages, all of its work stages are offered, so a failed load always has somewhere to go.
        /// </summary>
        public static List<string> ReworkChoices(JobOrder job, ShopSettings settings)
        {
            List<string> onRoute = WorkStages(job);
            string allowed = settings.ReworkStages ?? "";
            List<string> choices = onRoute.Where(stage => allowed.IndexOf(stage[0]) >= 0).ToList();
            return choices.Count > 0 ? choices : onRoute;
        }

        /// <summary>The table of allowed moves above, as code. Everything not listed is refused.</summary>
        public static bool IsAllowed(JobOrder job, string from, string to, ShopSettings settings)
        {
            if (from == Queued && to == Voided) return true;
            if (from == Inspection && ReworkChoices(job, settings).Contains(to)) return true;

            // Every other move is one step forward on the route: never skipping, never back.
            List<string> route = Route(job);
            int at = route.IndexOf(from);
            return at >= 0 && at < route.Count - 1 && route[at + 1] == to;
        }

        // ------------------------------------------------------------ Moving a job

        /// <summary>
        /// One step forward: start the first work stage, move to the next one, pass inspection,
        /// or hand over a load that is paid in full (Claimed; a load with a balance goes through
        /// Claim instead). <paramref name="expectedStatus"/> is the status the person saw on
        /// screen. Returns the new status. Passing inspection sends the ready email after the move
        /// is saved; a failed email never undoes the move.
        /// </summary>
        public static string Advance(int jobId, string expectedStatus, int userId)
        {
            string to;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = LockJob(conn, tx, jobId, expectedStatus);
                    to = NextStage(job);
                    if (to == null)
                        throw new BusinessRuleException("This job is " + job.CurrentStatus.ToLowerInvariant() + " and cannot move any more.");

                    Move(conn, tx, job, to, null, userId);
                    tx.Commit();
                }
            }

            if (to == Ready)
                EmailService.SendReady(jobId, userId, false);
            return to;
        }

        /// <summary>
        /// Hands the load over (Ready for pick-up -> Claimed) and, in the same transaction, takes
        /// whatever is still owed: the whole balance, in cash (at least the balance; the change is
        /// worked out by PaymentService) or by e-wallet. A load never leaves the shop unpaid.
        /// <paramref name="shownBalance"/> is the balance the page showed: if it changed in the
        /// meantime, nothing is saved. With no balance it is the same as Advance.
        /// </summary>
        public static void Claim(int jobId, decimal shownBalance, string method, decimal? cashReceived, int userId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = LockJob(conn, tx, jobId, Ready);
                    decimal balance = PaymentService.BalanceOf(conn, tx, jobId);
                    if (balance != shownBalance)
                        throw new BusinessRuleException("The balance is now " + PaymentService.Peso(balance)
                                                        + ". Check it with the customer, then try again.");
                    if (balance > 0)
                    {
                        decimal? cash = PaymentService.CheckIncoming(method, cashReceived, balance);
                        PaymentRepository.Insert(conn, tx, jobId, Payment.BalancePayment, balance, method, cash, userId);
                    }

                    Move(conn, tx, job, Claimed, null, userId);
                    tx.Commit();
                }
            }
        }

        /// <summary>
        /// A failed inspection: the load goes back to a work stage on its route to be redone.
        /// A reason is required. The timeline then shows the stage twice, never overwritten.
        /// </summary>
        public static void SendBack(int jobId, string toStage, string reason, int userId)
        {
            reason = CheckReason(reason, "Say why the load failed inspection.");
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = LockJob(conn, tx, jobId, Inspection);
                    if (!ReworkChoices(job, SettingsRepository.Get()).Contains(toStage))
                        throw new BusinessRuleException("This job's service does not include " + toStage + ".");

                    Move(conn, tx, job, toStage, reason, userId);
                    tx.Commit();
                }
            }
        }

        /// <summary>
        /// Cancels a job that is still Queued. A reason is required. The job is not deleted: it is
        /// marked Voided and keeps its row, the reason, who did it and when. Everything paid on it
        /// is refunded in the same transaction, as a negative payment in each method it was paid
        /// with, so the day's total and the cash drawer stay right.
        /// </summary>
        public static void Void(int jobId, string reason, int userId)
        {
            reason = CheckReason(reason, "Enter the reason for voiding this job.");
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = LockJob(conn, tx, jobId, Queued);
                    Move(conn, tx, job, Voided, reason, userId);

                    // What was paid, net of any earlier refund, read from vw_JobPayments.
                    JobPayments paid = PaymentRepository.GetJobTotals(conn, tx, jobId);
                    if (paid.CashPaid > 0)
                        PaymentRepository.Insert(conn, tx, jobId, Payment.VoidRefund, -paid.CashPaid, Payment.Cash, null, userId);
                    if (paid.EwalletPaid > 0)
                        PaymentRepository.Insert(conn, tx, jobId, Payment.VoidRefund, -paid.EwalletPaid, Payment.Ewallet, null, userId);

                    tx.Commit();
                }
            }

            // The customer is told it was cancelled, with the reason and what to do next. The job is
            // already voided and refunded; a failed email never undoes that.
            EmailService.SendVoided(jobId, userId, false);
        }

        // ------------------------------------------------------------ Pieces

        /// <summary>
        /// Reads the job with its row locked until the transaction ends, and checks it is still in
        /// the status the person saw. If not, someone else moved it first: nothing is saved.
        /// </summary>
        private static JobOrder LockJob(SqlConnection conn, SqlTransaction tx, int jobId, string expectedStatus)
        {
            JobOrder job = JobOrderRepository.GetForUpdate(conn, tx, jobId);
            if (job == null)
                throw new BusinessRuleException("That job could not be found.");
            if (job.CurrentStatus != expectedStatus)
                throw new BusinessRuleException(AlreadyUpdated);
            return job;
        }

        /// <summary>
        /// The move itself: the guarded UPDATE of CurrentStatus (0 rows = someone else moved the job
        /// first) and one new history row. Called inside the caller's transaction, so the two are
        /// saved together or not at all. A load with a balance is never marked Claimed.
        /// </summary>
        private static void Move(SqlConnection conn, SqlTransaction tx, JobOrder job, string to, string reason, int userId)
        {
            if (!IsAllowed(job, job.CurrentStatus, to, SettingsRepository.Get()))
                throw new BusinessRuleException("A job cannot go from " + job.CurrentStatus + " to " + to + ".");
            if (to == Claimed)
            {
                decimal balance = PaymentService.BalanceOf(conn, tx, job.JobId);
                if (balance > 0)
                    throw new BusinessRuleException("This load still has a " + PaymentService.Peso(balance)
                                                    + " balance. Collect it on the job page before handing the load over.");
            }
            if (!JobOrderRepository.UpdateStatus(conn, tx, job.JobId, job.CurrentStatus, to))
                throw new BusinessRuleException(AlreadyUpdated);
            StatusHistoryRepository.Insert(conn, tx, job.JobId, job.CurrentStatus, to, userId, reason);
        }

        private static string CheckReason(string reason, string whenMissing)
        {
            reason = (reason ?? "").Trim();
            if (reason == "") throw new BusinessRuleException(whenMissing);
            if (reason.Length > 200) throw new BusinessRuleException("Keep the reason to 200 characters.");
            return reason;
        }
    }
}

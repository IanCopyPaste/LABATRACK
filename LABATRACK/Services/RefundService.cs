using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography;
using LABATRACK.DataAccess;
using LABATRACK.Models;
using LABATRACK.Security;

namespace LABATRACK.Services
{
    /// <summary>
    /// The refund of a voided job. The rules, each one closing a way to be cheated:
    ///   - A refund is a record (Refunds table), not just a negative payment. It exists once per job and
    ///     is handed over exactly once: releasing it is a guarded UPDATE ... WHERE Status = 'Pending',
    ///     inside a transaction that locks the row. Showing the same claim number again, or two staff
    ///     pressing the button together, can never take it twice.
    ///   - If the customer is at the counter when the job is voided, the refund is handed over at once.
    ///     If not, it waits (Pending). The money has not left the drawer yet, so no negative payment
    ///     is written until it is handed over: then, on that day, by the person who hands it over.
    ///   - A waiting refund is collected with a refund code: 6 characters, emailed to the customer when
    ///     the job is voided. Only its PBKDF2 hash is stored (like a password), so nobody at the shop can
    ///     read it back. The claim number alone, which is printed on the slip, is not enough. Five wrong
    ///     codes lock the refund; after that only the owner can release it.
    ///   - The owner can release a refund without the code (a customer with no email, or a lost code),
    ///     but must give a reason, and it is recorded as an owner override with the owner's name.
    /// Every save here is one SqlTransaction: all of it is saved, or none of it.
    /// </summary>
    public static class RefundService
    {
        public const int MaxWrongCodes = 5;
        public const int CodeLength = 6;

        // No I, L, O, 0 or 1: they are easily mistaken for each other when a code is read out or typed.
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        // ------------------------------------------------------------ The code

        /// <summary>A new random code, for example K7M2QX, from a cryptographic random number generator.</summary>
        public static string NewCode()
        {
            var chars = new char[CodeLength];
            using (var rng = new RNGCryptoServiceProvider())
            {
                var one = new byte[1];
                int made = 0;
                // 31 characters divide 248 evenly, so a byte of 248 or more is skipped: no character is likelier than another.
                int usable = 256 - (256 % Alphabet.Length);
                while (made < CodeLength)
                {
                    rng.GetBytes(one);
                    if (one[0] < usable) chars[made++] = Alphabet[one[0] % Alphabet.Length];
                }
            }
            return new string(chars);
        }

        /// <summary>What was typed, in the form that is compared: capital letters and digits only ("k7m-2qx" becomes K7M2QX).</summary>
        public static string Normalize(string typed)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in (typed ?? "").ToUpperInvariant())
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) sb.Append(c);
            return sb.ToString();
        }

        /// <summary>The code as it is shown to the customer: K7M-2QX.</summary>
        public static string Format(string code)
        {
            return code.Length == CodeLength ? code.Substring(0, 3) + "-" + code.Substring(3) : code;
        }

        // ------------------------------------------------------------ Creating the refund (StatusService.Void)

        /// <summary>
        /// Records the refund of a job being voided, inside the void's own transaction.
        /// <paramref name="handedNow"/>: the customer is at the counter, so it is released in the same
        /// save. Otherwise it waits, and the new code is returned so the void email can carry it (null
        /// when handed now). <paramref name="paid"/> is what the job has paid, net.
        /// </summary>
        internal static string CreateForVoid(SqlConnection conn, SqlTransaction tx, int jobId, JobPayments paid,
                                             bool handedNow, int userId)
        {
            string code = null;
            byte[] salt = null, hash = null;
            if (!handedNow)
            {
                code = NewCode();
                salt = PasswordHasher.NewSalt();
                hash = PasswordHasher.Hash(code, salt);
            }

            int refundId = RefundRepository.Insert(conn, tx, jobId, paid.AmountPaid, hash, salt, userId);
            if (handedNow)
                Release(conn, tx, new Refund { RefundId = refundId, JobId = jobId, Amount = paid.AmountPaid }, Refund.AtVoid, null, userId);
            return code;
        }

        // ------------------------------------------------------------ Handing it over

        /// <summary>
        /// Hands over a waiting refund to the person who gives the right refund code. A wrong code is
        /// counted, and after <see cref="MaxWrongCodes"/> the refund is locked. Refused if the refund
        /// was already given.
        /// </summary>
        public static void ReleaseWithCode(int jobId, string code, int userId)
        {
            string refusal = null;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    Refund refund = Pending(conn, tx, jobId);
                    if (refund.FailedAttempts >= MaxWrongCodes)
                        throw new BusinessRuleException("This refund is locked after " + MaxWrongCodes
                                                        + " wrong codes. Only the owner can release it now.");

                    string typed = Normalize(code);
                    bool right = typed.Length == CodeLength && refund.CodeHash != null && refund.CodeSalt != null
                                 && PasswordHasher.Verify(typed, refund.CodeSalt, refund.CodeHash);
                    if (!right)
                    {
                        // The wrong try is saved even though the refund is refused (committed, then refused below).
                        int tries = RefundRepository.AddFailedAttempt(conn, tx, refund.RefundId);
                        tx.Commit();
                        int left = MaxWrongCodes - tries;
                        refusal = left > 0
                            ? "That refund code is not right. " + left + (left == 1 ? " try is" : " tries are") + " left before the refund is locked."
                            : "That refund code is not right. The refund is now locked: only the owner can release it.";
                    }
                    else
                    {
                        Release(conn, tx, refund, Refund.WithCode, null, userId);
                        tx.Commit();
                    }
                }
            }
            if (refusal != null) throw new BusinessRuleException(refusal);
        }

        /// <summary>
        /// The owner hands over a waiting refund without the code (a customer with no email, a lost
        /// code, or a locked refund). A reason is required, and the release is recorded as an owner
        /// override with the owner's name. Refused for anyone who is not the owner.
        /// </summary>
        public static void ReleaseByOwner(int jobId, string reason, int ownerUserId)
        {
            UserAccount user = UserRepository.GetById(ownerUserId);
            if (user == null || user.Role != "Owner" || !user.IsActive)
                throw new BusinessRuleException("Only the owner can release a refund without the code.");
            reason = (reason ?? "").Trim();
            if (reason == "") throw new BusinessRuleException("Say why the refund is released without the code (for example: no email address, ID checked).");
            if (reason.Length > 200) throw new BusinessRuleException("Keep the reason to 200 characters.");

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    Refund refund = Pending(conn, tx, jobId);
                    Release(conn, tx, refund, Refund.OwnerOverride, reason, ownerUserId);
                    tx.Commit();
                }
            }
        }

        /// <summary>
        /// Emails the customer a NEW refund code for a waiting refund (a lost email, or the first one
        /// never arrived). The old code stops working only once the new one has really been sent, so a
        /// failed email leaves the customer with the code they already have. Returns the email's result:
        /// Sent, Failed or Skipped (the customer has no email address).
        /// </summary>
        public static string ReissueCode(int jobId, int userId)
        {
            JobOrder job = JobOrderRepository.GetById(jobId);
            if (job == null || !job.RefundPending)
                throw new BusinessRuleException("This job has no refund waiting for a code.");

            string code = NewCode();
            string result = EmailService.SendVoided(jobId, userId, true, code);
            if (result != NotificationLogEntry.Sent) return result;

            byte[] salt = PasswordHasher.NewSalt();
            byte[] hash = PasswordHasher.Hash(code, salt);
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    Refund refund = Pending(conn, tx, jobId);   // refused if it was handed over while the email was going out
                    RefundRepository.SetCode(conn, tx, refund.RefundId, hash, salt);
                    tx.Commit();
                }
            }
            return result;
        }

        // ------------------------------------------------------------ Pieces

        /// <summary>The job's refund, locked, and required to be Pending; otherwise says why not.</summary>
        private static Refund Pending(SqlConnection conn, SqlTransaction tx, int jobId)
        {
            Refund refund = RefundRepository.GetForUpdate(conn, tx, jobId);
            if (refund == null)
                throw new BusinessRuleException("This job has no refund to give.");
            if (!refund.IsPending)
                throw new BusinessRuleException(AlreadyGiven(refund));
            return refund;
        }

        /// <summary>
        /// Marks the refund Released (only if it is still Pending) and writes the money going back out
        /// as negative payments, one per method it was paid with, dated now and by <paramref name="userId"/>,
        /// so the day's drawer figures stay right. Inside the caller's transaction.
        /// </summary>
        private static void Release(SqlConnection conn, SqlTransaction tx, Refund refund, string how, string reason, int userId)
        {
            if (!RefundRepository.Release(conn, tx, refund.RefundId, userId, how, reason))
                throw new BusinessRuleException("This refund was already given back. It can't be given twice.");

            JobPayments paid = PaymentRepository.GetJobTotals(conn, tx, refund.JobId);
            if (paid.AmountPaid != refund.Amount)
                throw new BusinessRuleException("The payments on this job (" + PaymentService.Peso(paid.AmountPaid)
                                                + ") no longer match its refund (" + PaymentService.Peso(refund.Amount) + "). Nothing was given back; ask the owner.");
            if (paid.CashPaid > 0)
                PaymentRepository.Insert(conn, tx, refund.JobId, Payment.VoidRefund, -paid.CashPaid, Payment.Cash, null, userId);
            if (paid.EwalletPaid > 0)
                PaymentRepository.Insert(conn, tx, refund.JobId, Payment.VoidRefund, -paid.EwalletPaid, Payment.Ewallet, null, userId);
        }

        /// <summary>"This refund was already given back on Sep 30, 2:15 PM by jcruz. It can't be given twice."</summary>
        public static string AlreadyGiven(Refund refund)
        {
            string when = refund.ReleasedAt.HasValue ? " on " + refund.ReleasedAt.Value.ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture) : "";
            string who = !string.IsNullOrEmpty(refund.ReleasedBy) ? " by " + refund.ReleasedBy : "";
            return "This refund was already given back" + when + who + ". It can't be given twice.";
        }
    }
}

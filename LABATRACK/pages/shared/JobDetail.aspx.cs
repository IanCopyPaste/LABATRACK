using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using LABATRACK.db_conn;
using LABATRACK.tools;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.pages.shared
{
    public partial class JobDetail : BasePage
    {
        protected override string PageStyleSheet
        {
            get { return "JobDetail.css"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private const int MaxWrongCodesAllowed = 5;
        private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        protected JobOrder Job { get; private set; }
        protected bool IsVoided { get; private set; }
        protected bool InShop { get; private set; }
        protected string NextStage { get; private set; }
        protected bool MinimumApplied { get; private set; }
        protected bool ReadyReached { get; private set; }
        protected bool NothingPaid { get; private set; }
        protected string RefundNote { get; private set; }
        protected string BackUrl { get; private set; }
        protected string BackText { get; private set; }

        protected string MoveTitle { get { return NextStage == null ? "" : MoveConfirm.Title(Job); } }
        protected string MoveText { get { return NextStage == null ? "" : MoveConfirm.Text(Job); } }

        private string VoidBase { get { return Job.CustomerName + " · " + Job.ServiceName + "\n\n"; } }
        private const string VoidEnd = " A voided job can't be undone; it stays in history with the reason.";

        protected string VoidTextNow
        {
            get
            {
                return VoidBase + (Job.AmountPaid > 0
                    ? "The job is cancelled and the " + Peso(Job.AmountPaid) + " paid is refunded now, the same way it was paid. Hand it to the customer."
                    : "The job is cancelled. Nothing was paid, so nothing is refunded.") + VoidEnd;
            }
        }

        protected string VoidTextLater
        {
            get
            {
                return VoidBase + "The job is cancelled. The " + Peso(Job.AmountPaid) + " paid is NOT handed out now: it waits for the customer, "
                     + (string.IsNullOrEmpty(Job.CustomerEmail)
                        ? "who has no email address, so no refund code can be sent and the owner will have to release it."
                        : "who is emailed a refund code and collects it at the counter. It can be given only once.") + VoidEnd;
            }
        }

        protected string VoidText { get { return VoidNowChecked || Job.AmountPaid <= 0 ? VoidTextNow : VoidTextLater; } }

        protected bool VoidNowChecked
        {
            get { return !(IsPostBack && Request.Form["action"] == "void") || Request.Form["voidNow"] == "1"; }
        }

        protected bool HasRefund { get { return IsVoided && Job.RefundStatus != null; } }

        protected string RefundHow
        {
            get
            {
                switch (Job.RefundReleasedHow)
                {
                    case Refund.AtVoid: return "Given back when the job was voided, with the customer at the counter.";
                    case Refund.WithCode: return "Given back to the customer who gave the refund code.";
                    case Refund.OwnerOverride: return "Released by the owner without the code.";
                    default: return "";
                }
            }
        }
        protected int MaxWrongCodes { get { return MaxWrongCodesAllowed; } }
        protected bool RefundLocked { get { return Job.RefundPending && Job.RefundFailedAttempts >= MaxWrongCodesAllowed; } }
        protected int WrongCodesLeft { get { return Math.Max(0, MaxWrongCodesAllowed - Job.RefundFailedAttempts); } }

        protected bool HasBalance { get { return InShop && Job.Balance > 0; } }

        protected bool CanCollect { get { return HasBalance && Job.CurrentStatus != JobStatus.Ready; } }

        protected string PaymentPill
        {
            get
            {
                if (IsVoided) return Job.RefundPending ? "pill-warn" : "pill-danger";
                if (Job.PaymentStatus == JobOrder.PaidStatus) return "pill-ok";
                if (Job.PaymentStatus == JobOrder.PartiallyPaidStatus)
                    return "pill-warn";
                else
                    return "pill-danger";
            }
        }

        protected string PaymentPillText
        {
            get
            {
                if (IsVoided) return Job.RefundPending ? "Refund waiting" : Job.RefundStatus == null ? "Voided" : "Refunded";
                if (Job.PaymentStatus == JobOrder.PaidStatus)
                    return "Paid in full";
                else
                    return Job.PaymentStatus;
            }
        }

        private int jobId;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!int.TryParse(Request.QueryString["id"], out jobId) || ReadJob() == null)
            {
                Response.Redirect("History.aspx", true);
                return;
            }

            if (IsPostBack)
                HandleAction();
            else
                ShowDoneNotice();

            Bind();
        }

        private void HandleAction()
        {
            string shown = hfShownStatus.Value;
            int userId = CurrentUser.UserId;
            string error = null;

            switch (Request.Form["action"])
            {
                case "advance":
                    error = Advance(shown, userId);
                    if (error == null) Done("moved");
                    break;

                case "rework":
                    error = SendBack(ddlReworkStage.SelectedValue, txtReworkReason.Text, userId);
                    if (error == null) Done("rework");
                    break;

                case "void":
                    string refundCode;
                    error = Void(txtVoidReason.Text, Request.Form["voidNow"] == "1", userId, out refundCode);
                    if (error == null)
                    {
                        Email.SendVoided(jobId, userId, false, refundCode);
                        Done("voided");
                    }
                    break;

                case "release-refund":
                    error = ReleaseWithCode(Request.Form["refundCode"], userId);
                    if (error == null) Done("refunded");
                    break;

                case "release-override":
                    error = ReleaseByOwner(Request.Form["overrideReason"], userId);
                    if (error == null) Done("refunded");
                    break;

                case "reissue-code":
                    string result;
                    error = ReissueCode(userId, out result);
                    if (error == null) Done(NewCodeResult(result));
                    break;

                case "pay":
                    error = Collect(userId);
                    if (error == null) Done("paid");
                    break;

                case "claim":
                    error = Claim(userId);
                    if (error == null) Done("moved");
                    break;

                case "resend-dropoff":
                    if (ReadJob().IsInShop == false)
                    {
                        error = "This job is closed, so its emails are not sent again.";
                    }
                    else
                    {
                        Email.SendDropOff(jobId, userId, true);
                        Done("resent");
                    }
                    break;

                case "resend-ready":
                    if (ReadJob().IsInShop == false)
                    {
                        error = "This job is closed, so its emails are not sent again.";
                    }
                    else
                    {
                        Email.SendReady(jobId, userId, true);
                        Done("resent");
                    }
                    break;

                case "resend-voided":
                    JobOrder voided = ReadJob();
                    if (voided.CurrentStatus != JobStatus.Voided)
                    {
                        error = "Only a voided job has a cancellation email.";
                    }
                    else if (voided.RefundPending)
                    {
                        string newCodeResult;
                        error = ReissueCode(userId, out newCodeResult);
                        if (error == null) Done(NewCodeResult(newCodeResult));
                    }
                    else
                    {
                        Email.SendVoided(jobId, userId, true, null);
                        Done("resent");
                    }
                    break;
            }

            if (error != null)
                ShowNotice("warn", error);
        }

        private string Advance(string expectedStatus, int userId)
        {
            string error = null;
            string to = null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = LockJob(conn, tx);
                    if (job == null)
                        error = "That job could not be found.";
                    else if (job.CurrentStatus != expectedStatus)
                        error = JobStatus.AlreadyUpdated;
                    else
                    {
                        to = job.NextStage;
                        if (to == null)
                            error = "This job is " + job.CurrentStatus.ToLowerInvariant() + " and cannot move any more.";
                        else
                            error = MoveJob(conn, tx, job, to, null, userId);
                    }

                    if (error == null)
                        tx.Commit();
                }
            }

            if (error == null && to == JobStatus.Ready)
                Email.SendReady(jobId, userId, false);
            return error;
        }

        private string SendBack(string toStage, string reason, int userId)
        {
            reason = (reason ?? "").Trim();
            if (reason == "") return "Say why the load failed inspection.";
            if (reason.Length > 200) return "Keep the reason to 200 characters.";

            string reworkStages = ReadReworkStages();
            string error = null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = LockJob(conn, tx);
                    if (job == null)
                        error = "That job could not be found.";
                    else if (job.CurrentStatus != JobStatus.Inspection)
                        error = JobStatus.AlreadyUpdated;
                    else if (!ReworkChoices(job, reworkStages).Contains(toStage))
                        error = "This job's service does not include " + toStage + ".";
                    else
                        error = MoveJob(conn, tx, job, toStage, reason, userId);

                    if (error == null)
                        tx.Commit();
                }
            }
            return error;
        }

        private string Void(string reason, bool refundNow, int userId, out string refundCode)
        {
            refundCode = null;
            reason = (reason ?? "").Trim();
            if (reason == "") return "Enter the reason for voiding this job.";
            if (reason.Length > 200) return "Keep the reason to 200 characters.";

            string error = null;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = LockJob(conn, tx);
                    if (job == null)
                        error = "That job could not be found.";
                    else if (job.CurrentStatus != JobStatus.Queued)
                        error = JobStatus.AlreadyUpdated;
                    else
                        error = MoveJob(conn, tx, job, JobStatus.Voided, reason, userId);

                    if (error == null)
                    {
                        JobPayments paid = ReadTotals(conn, tx);
                        if (paid.AmountPaid > 0)
                        {
                            byte[] salt = null;
                            byte[] hash = null;
                            if (!refundNow)
                            {
                                refundCode = NewCode();
                                salt = PasswordHasher.NewSalt();
                                hash = PasswordHasher.Hash(refundCode, salt);
                            }

                            int refundId;
                            using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_Insert", conn, tx))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                                cmd.Parameters.Add(new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = paid.AmountPaid });
                                cmd.Parameters.Add("@CodeHash", SqlDbType.VarBinary, 32).Value = hash == null ? (object)DBNull.Value : hash;
                                cmd.Parameters.Add("@CodeSalt", SqlDbType.VarBinary, 16).Value = salt == null ? (object)DBNull.Value : salt;
                                cmd.Parameters.Add("@CreatedByUserId", SqlDbType.Int).Value = userId;

                                refundId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            if (refundNow)
                                error = ReleaseRefund(conn, tx, refundId, paid.AmountPaid, Refund.AtVoid, null, userId);
                        }
                    }

                    if (error == null)
                        tx.Commit();
                }
            }

            if (error != null)
                refundCode = null;
            return error;
        }

        private string ReleaseWithCode(string code, int userId)
        {
            string refusal = null;
            string error = null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    Refund refund = LockRefund(conn, tx);
                    if (refund == null)
                    {
                        error = "This job has no refund to give.";
                    }
                    else if (!refund.IsPending)
                    {
                        error = AlreadyGiven(refund);
                    }
                    else if (refund.FailedAttempts >= MaxWrongCodesAllowed)
                    {
                        error = "This refund is locked after " + MaxWrongCodesAllowed + " wrong codes. Only the owner can release it now.";
                    }
                    else
                    {
                        string typed = "";
                        foreach (char c in (code ?? "").ToUpperInvariant())
                        {
                            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) typed += c;
                        }

                        bool right = typed.Length == 6 && refund.CodeHash != null && refund.CodeSalt != null
                                     && PasswordHasher.Verify(typed, refund.CodeSalt, refund.CodeHash);

                        if (!right)
                        {
                            int tries;
                            using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_AddFailedAttempt", conn, tx))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@RefundId", SqlDbType.Int).Value = refund.RefundId;

                                tries = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            tx.Commit();

                            int left = MaxWrongCodesAllowed - tries;
                            refusal = left > 0
                                ? "That refund code is not right. " + left + (left == 1 ? " try is" : " tries are") + " left before the refund is locked."
                                : "That refund code is not right. The refund is now locked: only the owner can release it.";
                        }
                        else
                        {
                            error = ReleaseRefund(conn, tx, refund.RefundId, refund.Amount, Refund.WithCode, null, userId);
                            if (error == null)
                                tx.Commit();
                        }
                    }
                }
            }

            return refusal ?? error;
        }

        private string ReleaseByOwner(string reason, int ownerUserId)
        {
            if (!IsOwner)
                return "Only the owner can release a refund without the code.";

            reason = (reason ?? "").Trim();
            if (reason == "") return "Say why the refund is released without the code (for example: no email address, ID checked).";
            if (reason.Length > 200) return "Keep the reason to 200 characters.";

            string error = null;
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    Refund refund = LockRefund(conn, tx);
                    if (refund == null)
                        error = "This job has no refund to give.";
                    else if (!refund.IsPending)
                        error = AlreadyGiven(refund);
                    else
                        error = ReleaseRefund(conn, tx, refund.RefundId, refund.Amount, Refund.OwnerOverride, reason, ownerUserId);

                    if (error == null)
                        tx.Commit();
                }
            }
            return error;
        }

        private string ReissueCode(int userId, out string result)
        {
            result = null;
            JobOrder job = ReadJob();
            if (job == null || !job.RefundPending)
                return "This job has no refund waiting for a code.";

            string code = NewCode();
            result = Email.SendVoided(jobId, userId, true, code);
            if (result != NotificationLogEntry.Sent)
                return null;

            byte[] salt = PasswordHasher.NewSalt();
            byte[] hash = PasswordHasher.Hash(code, salt);
            string error = null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    Refund refund = LockRefund(conn, tx);
                    if (refund == null)
                    {
                        error = "This job has no refund to give.";
                    }
                    else if (!refund.IsPending)
                    {
                        error = AlreadyGiven(refund);
                    }
                    else
                    {
                        using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_SetCode", conn, tx))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@RefundId", SqlDbType.Int).Value = refund.RefundId;
                            cmd.Parameters.Add("@CodeHash", SqlDbType.VarBinary, 32).Value = hash;
                            cmd.Parameters.Add("@CodeSalt", SqlDbType.VarBinary, 16).Value = salt;

                            cmd.ExecuteScalar();
                        }
                        tx.Commit();
                    }
                }
            }
            return error;
        }

        private string Collect(int userId)
        {
            decimal amount;
            if (!TryPesos(Request.Form["payAmount"], out amount))
                return "Enter the amount paid in pesos, for example 100 or 100.00.";
            if (amount < 0 || amount != Math.Round(amount, 2) || amount > 99999999m)
                return "Enter the amount paid in pesos, for example 100 or 100.00.";
            if (amount <= 0)
                return "Enter the amount the customer is paying.";

            string method = Request.Form["payMethod"];
            decimal? cashReceived = null;
            if (method == Payment.Cash)
            {
                decimal cash;
                if (TryPesos(Request.Form["payCash"], out cash))
                    cashReceived = cash;
            }
            decimal shownBalance = ShownBalance();
            string error = null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = LockJob(conn, tx);
                    if (job == null)
                    {
                        error = "That job could not be found.";
                    }
                    else if (!job.IsInShop)
                    {
                        error = "A " + job.CurrentStatus.ToLowerInvariant() + " job takes no more payments.";
                    }
                    else
                    {
                        JobPayments paid = ReadTotals(conn, tx);
                        decimal balance = paid.Balance;

                        if (balance != shownBalance)
                            error = "The balance is now " + Peso(balance) + ". Check it with the customer, then try again.";
                        else if (balance <= 0)
                            error = "This job is already paid in full.";
                        else if (amount > balance)
                            error = "The balance is only " + Peso(balance) + ". Enter at most that.";
                        else
                        {
                            decimal? cashToStore = null;
                            if (method == Payment.Ewallet)
                                cashToStore = null;
                            else if (method != Payment.Cash)
                                error = "Choose how the customer paid: Cash or E-wallet.";
                            else if (!cashReceived.HasValue || cashReceived.Value < amount)
                                error = "Cash received must be at least the " + Peso(amount) + " being paid.";
                            else if (cashReceived.Value < 0 || cashReceived.Value != Math.Round(cashReceived.Value, 2) || cashReceived.Value > 99999999m)
                                error = "Enter the cash received in pesos, for example 500 or 500.00.";
                            else
                                cashToStore = cashReceived.Value;

                            if (error == null)
                            {
                                InsertPayment(conn, tx, Payment.BalancePayment, amount, method, cashToStore, userId);
                                tx.Commit();
                            }
                        }
                    }
                }
            }
            return error;
        }

        private string Claim(int userId)
        {
            string method = Request.Form["claimMethod"];
            decimal? cashReceived = null;
            if (method == Payment.Cash)
            {
                decimal cash;
                if (TryPesos(Request.Form["claimCashReceived"], out cash))
                    cashReceived = cash;
            }
            decimal shownBalance = ShownBalance();
            string error = null;

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    JobOrder job = LockJob(conn, tx);
                    if (job == null)
                    {
                        error = "That job could not be found.";
                    }
                    else if (job.CurrentStatus != JobStatus.Ready)
                    {
                        error = JobStatus.AlreadyUpdated;
                    }
                    else
                    {
                        decimal balance = ReadTotals(conn, tx).Balance;
                        if (balance != shownBalance)
                        {
                            error = "The balance is now " + Peso(balance) + ". Check it with the customer, then try again.";
                        }
                        else
                        {
                            if (balance > 0)
                            {
                                decimal? cashToStore = null;
                                if (method == Payment.Ewallet)
                                    cashToStore = null;
                                else if (method != Payment.Cash)
                                    error = "Choose how the customer paid: Cash or E-wallet.";
                                else if (!cashReceived.HasValue || cashReceived.Value < balance)
                                    error = "Cash received must be at least the " + Peso(balance) + " being paid.";
                                else if (cashReceived.Value < 0 || cashReceived.Value != Math.Round(cashReceived.Value, 2) || cashReceived.Value > 99999999m)
                                    error = "Enter the cash received in pesos, for example 500 or 500.00.";
                                else
                                    cashToStore = cashReceived.Value;

                                if (error == null)
                                    InsertPayment(conn, tx, Payment.BalancePayment, balance, method, cashToStore, userId);
                            }

                            if (error == null)
                                error = MoveJob(conn, tx, job, JobStatus.Claimed, null, userId);
                        }
                    }

                    if (error == null)
                        tx.Commit();
                }
            }
            return error;
        }

        private JobOrder LockJob(SqlConnection conn, SqlTransaction tx)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetForUpdate", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return JobOrder.FromRow(r);
                    return null;
                }
            }
        }

        private string MoveJob(SqlConnection conn, SqlTransaction tx, JobOrder job, string to, string reason, int userId)
        {
            if (to == JobStatus.Claimed)
            {
                decimal balance = ReadTotals(conn, tx).Balance;
                if (balance > 0)
                    return "This load still has a " + Peso(balance)
                         + " balance. Collect it on the job page before handing the load over.";
            }

            int rowsAffected;
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_UpdateStatus", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = job.JobId;
                cmd.Parameters.Add("@ExpectedStatus", SqlDbType.NVarChar, 20).Value = job.CurrentStatus;
                cmd.Parameters.Add("@NewStatus", SqlDbType.NVarChar, 20).Value = to;

                rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
            }
            if (rowsAffected != 1)
                return JobStatus.AlreadyUpdated;

            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobStatusHistory_Insert", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = job.JobId;
                cmd.Parameters.Add("@FromStatus", SqlDbType.NVarChar, 20).Value = job.CurrentStatus;
                cmd.Parameters.Add("@ToStatus", SqlDbType.NVarChar, 20).Value = to;
                cmd.Parameters.Add("@ChangedByUserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = reason == null ? (object)DBNull.Value : reason;

                cmd.ExecuteNonQuery();
            }
            return null;
        }

        private JobPayments ReadTotals(SqlConnection conn, SqlTransaction tx)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetJobTotals", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;

                    JobPayments totals = new JobPayments();
                    totals.JobId = (int)r["JobId"];
                    totals.TotalAmount = (decimal)r["TotalAmount"];
                    totals.AmountPaid = (decimal)r["AmountPaid"];
                    totals.CashPaid = (decimal)r["CashPaid"];
                    totals.EwalletPaid = (decimal)r["EwalletPaid"];
                    totals.PaymentCount = Convert.ToInt32(r["PaymentCount"]);
                    return totals;
                }
            }
        }

        private void InsertPayment(SqlConnection conn, SqlTransaction tx, string kind, decimal amount, string method, decimal? cashReceived, int userId)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_Insert", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@Kind", SqlDbType.NVarChar, 12).Value = kind;
                cmd.Parameters.Add(new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = amount });
                cmd.Parameters.Add("@Method", SqlDbType.NVarChar, 10).Value = method;
                cmd.Parameters.Add(new SqlParameter("@CashReceived", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = cashReceived.HasValue ? (object)cashReceived.Value : DBNull.Value });
                cmd.Parameters.Add("@RecordedByUserId", SqlDbType.Int).Value = userId;

                cmd.ExecuteNonQuery();
            }
        }

        private Refund LockRefund(SqlConnection conn, SqlTransaction tx)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_GetForUpdate", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;

                    Refund refund = new Refund();
                    refund.RefundId = (int)r["RefundId"];
                    refund.JobId = (int)r["JobId"];
                    refund.Amount = (decimal)r["Amount"];
                    refund.Status = (string)r["Status"];
                    refund.CodeHash = r["CodeHash"] as byte[];
                    refund.CodeSalt = r["CodeSalt"] as byte[];
                    refund.FailedAttempts = (int)r["FailedAttempts"];
                    refund.CreatedAt = (DateTime)r["CreatedAt"];
                    refund.ReleasedAt = r["ReleasedAt"] as DateTime?;
                    refund.ReleasedBy = r["ReleasedBy"] as string;
                    refund.ReleasedHow = r["ReleasedHow"] as string;
                    return refund;
                }
            }
        }

        private string ReleaseRefund(SqlConnection conn, SqlTransaction tx, int refundId, decimal refundAmount, string how, string reason, int userId)
        {
            int rowsAffected;
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_Release", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RefundId", SqlDbType.Int).Value = refundId;
                cmd.Parameters.Add("@ReleasedByUserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@ReleasedHow", SqlDbType.NVarChar, 16).Value = how;
                cmd.Parameters.Add("@OverrideReason", SqlDbType.NVarChar, 200).Value = reason == null ? (object)DBNull.Value : reason;

                rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
            }
            if (rowsAffected != 1)
                return "This refund was already given back. It can't be given twice.";

            JobPayments paid = ReadTotals(conn, tx);
            if (paid.AmountPaid != refundAmount)
                return "The payments on this job (" + Peso(paid.AmountPaid) + ") no longer match its refund (" + Peso(refundAmount)
                     + "). Nothing was given back; ask the owner.";

            if (paid.CashPaid > 0)
                InsertPayment(conn, tx, Payment.VoidRefund, -paid.CashPaid, Payment.Cash, null, userId);
            if (paid.EwalletPaid > 0)
                InsertPayment(conn, tx, Payment.VoidRefund, -paid.EwalletPaid, Payment.Ewallet, null, userId);
            return null;
        }

        private static string AlreadyGiven(Refund refund)
        {
            string when;
            if (refund.ReleasedAt.HasValue)
                when = " on " + refund.ReleasedAt.Value.ToString("MMM d, h:mm tt", Inv);
            else
                when = "";
            string who;
            if (!string.IsNullOrEmpty(refund.ReleasedBy))
                who = " by " + refund.ReleasedBy;
            else
                who = "";
            return "This refund was already given back" + when + who + ". It can't be given twice.";
        }

        private static string NewCode()
        {
            char[] chars = new char[6];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] one = new byte[1];
                int made = 0;
                int usable = 256 - (256 % CodeAlphabet.Length);
                while (made < 6)
                {
                    rng.GetBytes(one);
                    if (one[0] < usable)
                    {
                        chars[made] = CodeAlphabet[one[0] % CodeAlphabet.Length];
                        made++;
                    }
                }
            }
            return new string(chars);
        }

        private static string NewCodeResult(string emailResult)
        {
            return emailResult == NotificationLogEntry.Sent ? "codesent" : emailResult == NotificationLogEntry.Skipped ? "codeskipped" : "codefailed";
        }

        private decimal ShownBalance()
        {
            decimal shown;
            if (decimal.TryParse(hfShownBalance.Value, NumberStyles.Number, Inv, out shown))
                return shown;
            else
                return -1m;
        }

        private static bool TryPesos(string text, out decimal amount)
        {
            text = (text ?? "").Replace(",", "").Replace("₱", "").Trim();
            return decimal.TryParse(text, NumberStyles.Number, Inv, out amount);
        }

        private string ReadReworkStages()
        {
            string stages = "WDF";
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        if ((string)r["SettingKey"] == ShopSettings.ReworkStagesKey && r["SettingValue"] as string != null)
                            stages = (string)r["SettingValue"];
                    }
                }
            }
            return stages;
        }

        private static List<string> ReworkChoices(JobOrder job, string reworkStages)
        {
            List<string> onRoute = job.WorkStages;
            List<string> choices = onRoute.Where(stage => reworkStages.IndexOf(stage[0]) >= 0).ToList();
            if (choices.Count > 0)
                return choices;
            else
                return onRoute;
        }

        private JobOrder ReadJob()
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return JobOrder.FromRow(r);
                    return null;
                }
            }
        }

        private List<Payment> ReadPayments()
        {
            List<Payment> payments = new List<Payment>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetByJob", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Payment payment = new Payment();
                        payment.PaymentId = (int)r["PaymentId"];
                        payment.JobId = (int)r["JobId"];
                        payment.Kind = (string)r["Kind"];
                        payment.Amount = (decimal)r["Amount"];
                        payment.Method = (string)r["Method"];
                        payment.CashReceived = r["CashReceived"] as decimal?;
                        payment.PaidAt = (DateTime)r["PaidAt"];
                        payment.RecordedByUserId = (int)r["RecordedByUserId"];
                        payment.RecordedBy = (string)r["RecordedBy"];
                        payments.Add(payment);
                    }
                }
            }
            return payments;
        }

        private void Done(string what)
        {
            Response.Redirect("JobDetail.aspx?id=" + jobId + "&done=" + what, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void Bind()
        {
            Job = ReadJob();
            IsVoided = Job.CurrentStatus == JobStatus.Voided;
            InShop = Job.IsInShop;
            NextStage = Job.NextStage;

            decimal billable = Math.Ceiling(Job.WeightKg / Job.RoundingIncrementKgAtCreation) * Job.RoundingIncrementKgAtCreation;
            decimal chargeBeforeMinimum = Math.Round(billable * Job.RatePerKgAtCreation, 2, MidpointRounding.AwayFromZero);
            MinimumApplied = chargeBeforeMinimum < Job.MinimumChargeAtCreation;

            hfShownStatus.Value = Job.CurrentStatus;
            hfShownBalance.Value = Money(Job.Balance);
            Title = "Job " + Job.ClaimNumber + " · LabaTrack";
            BackUrl = ResolveUrl(IsOwner ? "~/admin/Dashboard.aspx" : "~/pages/shared/Dashboard.aspx");
            if (IsOwner)
                BackText = "Back to dashboard";
            else
                BackText = "Back to job board";

            List<StatusChange> history = new List<StatusChange>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobStatusHistory_GetByJob", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        StatusChange change = new StatusChange();
                        change.HistoryId = (int)r["HistoryId"];
                        change.JobId = (int)r["JobId"];
                        change.FromStatus = r["FromStatus"] as string;
                        change.ToStatus = (string)r["ToStatus"];
                        change.ChangedAt = (DateTime)r["ChangedAt"];
                        change.ChangedByUserId = (int)r["ChangedByUserId"];
                        change.ChangedBy = (string)r["ChangedBy"];
                        change.Reason = r["Reason"] as string;
                        history.Add(change);
                    }
                }
            }
            ReadyReached = history.Any(h => h.ToStatus == JobStatus.Ready);
            rptStepper.DataSource = BuildSteps(history);
            rptStepper.DataBind();
            rptHistory.DataSource = history.AsEnumerable().Reverse();
            rptHistory.DataBind();

            List<JobAddOn> lines = new List<JobAddOn>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_GetByJob", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

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
            rptLines.DataSource = lines;
            rptLines.DataBind();

            List<Payment> payments = ReadPayments();
            rptPayments.DataSource = payments;
            rptPayments.DataBind();
            NothingPaid = payments.Count == 0;
            RefundNote = BuildRefundNote(Job, payments);

            List<NotificationLogEntry> emails = new List<NotificationLogEntry>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_NotificationLog_GetByJob", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        NotificationLogEntry entry = new NotificationLogEntry();
                        entry.NotificationId = (int)r["NotificationId"];
                        entry.JobId = (int)r["JobId"];
                        entry.Kind = (string)r["Kind"];
                        entry.Recipient = r["Recipient"] as string;
                        entry.Status = (string)r["Status"];
                        entry.Detail = r["Detail"] as string;
                        entry.IsResend = (bool)r["IsResend"];
                        entry.AttemptedAt = (DateTime)r["AttemptedAt"];
                        entry.AttemptedByUserId = (int)r["AttemptedByUserId"];
                        entry.AttemptedBy = (string)r["AttemptedBy"];
                        emails.Add(entry);
                    }
                }
            }
            rptEmails.DataSource = emails;
            rptEmails.DataBind();

            string status = Job.CurrentStatus;
            pnlQueued.Visible = status == JobStatus.Queued;
            pnlAdvance.Visible = Job.WorkStages.Contains(status);
            pnlInspection.Visible = status == JobStatus.Inspection;
            pnlReady.Visible = status == JobStatus.Ready;
            pnlClosed.Visible = !InShop;

            if (pnlInspection.Visible && ddlReworkStage.Items.Count == 0)
            {
                foreach (string stage in ReworkChoices(Job, ReadReworkStages()))
                    ddlReworkStage.Items.Add(stage);
                ddlReworkStage.SelectedIndex = ddlReworkStage.Items.Count - 1;
            }
        }

        private List<StepItem> BuildSteps(List<StatusChange> history)
        {
            List<string> route = Job.Route;
            int current = route.IndexOf(Job.CurrentStatus);
            bool claimed = Job.CurrentStatus == JobStatus.Claimed;

            List<StepItem> steps = new List<StepItem>();
            for (int i = 0; i < route.Count; i++)
            {
                StepItem step = new StepItem();
                step.Name = route[i];
                step.Number = i + 1;
                if (IsVoided)
                    if (i == 0)
                        step.State = "done";
                    else
                        step.State = "future";
                else if (i < current || claimed)
                    step.State = "done";
                else if (i == current)
                    step.State = "current";
                else
                    step.State = "future";

                StatusChange entered = history.LastOrDefault(h => h.ToStatus == route[i]);
                if (entered != null && step.State != "future")
                {
                    step.When = When(entered.ChangedAt);
                    step.By = entered.ChangedBy;
                }
                steps.Add(step);
            }
            return steps;
        }

        private static string BuildRefundNote(JobOrder job, List<Payment> payments)
        {
            if (job.RefundPending)
                return Peso(job.RefundAmount) + " is waiting for the customer to collect it.";
            List<string> parts = payments.Where(p => p.Kind == Payment.VoidRefund)
                                         .Select(p => Peso(-p.Amount) + (p.Method == Payment.Cash ? " in cash" : " by e-wallet"))
                                         .ToList();
            if (parts.Count == 0) return "Nothing had been paid, so nothing was refunded.";
            return "Refunded " + string.Join(" and ", parts)
                 + (job.RefundReleasedAt.HasValue ? " on " + job.RefundReleasedAt.Value.ToString("MMM d, h:mm tt", Inv) : "")
                 + (!string.IsNullOrEmpty(job.RefundReleasedBy) ? " by " + job.RefundReleasedBy : "") + ".";
        }

        private void ShowDoneNotice()
        {
            JobOrder job = ReadJob();
            string text;
            switch (Request.QueryString["done"])
            {
                case "moved":
                    if (job.CurrentStatus == JobStatus.Ready)
                        text = "Passed inspection: ready for pick-up. The ready email is listed under Notifications.";
                    else if (job.CurrentStatus == JobStatus.Claimed)
                        text = "Marked as claimed: paid in full and handed over.";
                    else
                        text = "Moved to " + job.CurrentStatus + ".";
                    break;
                case "paid":
                    text = job.Balance > 0
                        ? "Payment recorded. " + Peso(job.Balance) + " is still owed, collected when the load is claimed."
                        : "Payment recorded. The job is now paid in full.";
                    break;
                case "rework": text = "Sent back to " + job.CurrentStatus + " to be redone."; break;
                case "voided":
                    text = "Voided. " + BuildRefundNote(job, ReadPayments()) + " The cancellation email is listed under Notifications."
                         + (job.RefundPending && string.IsNullOrEmpty(job.CustomerEmail)
                            ? " This customer has no email address, so no refund code could be sent: the owner has to release the refund when they come."
                            : "");
                    break;
                case "refunded": text = BuildRefundNote(job, ReadPayments()) + " It is recorded, and it can't be given again."; break;
                case "codesent": text = "A new refund code was emailed to the customer. The old code no longer works."; break;
                case "codeskipped":
                    ShowNotice("warn", "This customer has no email address, so no new code could be sent. The old code still works; the owner can release the refund without one.");
                    return;
                case "codefailed":
                    ShowNotice("warn", "The new code could not be emailed (see Notifications), so the old code still works. Try again, or ask the owner to release the refund.");
                    return;
                case "resent": text = "Email tried again. The result is listed under Notifications."; break;
                case "edited":
                    text = "Correction saved. " + (job.Balance > 0
                        ? "The balance is now " + Peso(job.Balance) + ", collected before pick-up."
                        : "Nothing is owed on this job. A refund, if one was due, is listed under Payment.")
                        + " The customer's update email is listed under Notifications.";
                    break;
                default: return;
            }
            ShowNotice("info", text);
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

        protected static string Peso(decimal amount)
        {
            return (amount < 0 ? "−₱" : "₱") + Math.Abs(amount).ToString("N2", Inv);
        }

        protected static string Kg(decimal kg)
        {
            return kg.ToString("0.0#", Inv);
        }

        protected static string When(DateTime at)
        {
            return at.ToString("MMM d, h:mm tt", Inv);
        }

        protected static decimal LineAmount(JobAddOn line)
        {
            return Math.Round(line.Quantity * line.PriceAtCreation, 2, MidpointRounding.AwayFromZero);
        }

        protected static string PaymentLabel(Payment p)
        {
            if (p.Kind == Payment.DropOff) return "Paid at drop-off · " + p.Method;
            if (p.Kind == Payment.BalancePayment) return "Balance payment · " + p.Method;
            if (p.Kind == Payment.VoidRefund) return "Refund on void · " + p.Method;
            return (p.Amount > 0 ? "Collected on correction · " : "Refunded on correction · ") + p.Method;
        }

        protected static string CashNote(Payment p)
        {
            if (p.CashReceived.HasValue)
                return " · cash " + Peso(p.CashReceived.Value) + ", change " + Peso(p.Change);
            else
                return "";
        }

        protected string Posted(string name, string fallback)
        {
            if (IsPostBack && Request.Form[name] != null)
                return Request.Form[name];
            else
                return fallback;
        }

        protected bool IsPosted(string name, string value)
        {
            return IsPostBack && Request.Form[name] == value;
        }

        protected static string Money(decimal amount)
        {
            return amount.ToString("0.00", Inv);
        }

        protected static string EmailDetail(NotificationLogEntry n)
        {
            string who;
            if (n.IsResend)
                who = " · resent by " + n.AttemptedBy;
            else
                who = "";
            if (n.Status == NotificationLogEntry.Sent) return "to " + n.Recipient + who;
            return (n.Detail ?? n.Status) + who;
        }

        protected static string EmailPill(string status)
        {
            if (status == NotificationLogEntry.Sent) return "pill-ok";
            if (status == NotificationLogEntry.Failed) return "pill-danger";
            return "st-claimed";
        }

        protected static string EmailDotStyle(string status)
        {
            return status == NotificationLogEntry.Failed
                ? "background:var(--danger-soft); color:var(--danger);"
                : "background:var(--primary-soft); color:var(--primary);";
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

        public class StepItem
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public string State { get; set; }
            public string When { get; set; }
            public string By { get; set; }
        }
    }
}

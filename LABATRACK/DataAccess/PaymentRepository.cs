using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The Payments table and the vw_JobPayments view. The payment rules (the minimum down
    /// payment, cash received at least the amount, the balance paid before claiming, refunds on
    /// void) are in PaymentService, JobOrderService and StatusService.
    /// </summary>
    public static class PaymentRepository
    {
        /// <summary>Adds one payment (or refund) row as a step of a larger save (the connection and transaction come from the caller).</summary>
        public static void Insert(SqlConnection conn, SqlTransaction tx, int jobId, string kind, decimal amount,
                                  string method, decimal? cashReceived, int recordedByUserId)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_Insert", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@Kind", SqlDbType.NVarChar, 12).Value = kind;
                cmd.Parameters.Add(new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = amount });
                cmd.Parameters.Add("@Method", SqlDbType.NVarChar, 10).Value = method;
                cmd.Parameters.Add(new SqlParameter("@CashReceived", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = (object)cashReceived ?? DBNull.Value });
                cmd.Parameters.Add("@RecordedByUserId", SqlDbType.Int).Value = recordedByUserId;

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>A job's payments and refunds, oldest first.</summary>
        public static List<Payment> GetByJob(int jobId)
        {
            List<Payment> list = new List<Payment>();
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
                        list.Add(Map(r));
                    }
                }
            }
            return list;
        }

        /// <summary>What has been paid on a job, net of refunds, from vw_JobPayments.</summary>
        public static JobPayments GetJobTotals(int jobId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                return GetJobTotals(conn, null, jobId);
            }
        }

        /// <summary>The same, read inside a save's transaction (the connection and transaction come from the caller).</summary>
        public static JobPayments GetJobTotals(SqlConnection conn, SqlTransaction tx, int jobId)
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

        /// <summary>Money in and refunded within [from, to), by method, counted on the payment date.</summary>
        public static DaySales GetDaySummary(DateTime from, DateTime to)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetDaySummary", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    r.Read();   // the procedure always returns exactly one row of totals

                    DaySales sales = new DaySales();
                    sales.CashIn = Convert.ToDecimal(r["CashIn"]);
                    sales.CashRefunds = Convert.ToDecimal(r["CashRefunds"]);
                    sales.EwalletIn = Convert.ToDecimal(r["EwalletIn"]);
                    sales.EwalletRefunds = Convert.ToDecimal(r["EwalletRefunds"]);
                    sales.PaymentCount = Convert.ToInt32(r["PaymentCount"]);
                    sales.RefundCount = Convert.ToInt32(r["RefundCount"]);
                    return sales;
                }
            }
        }

        /// <summary>Every payment and refund within [from, to) with its job: the daily ledger.</summary>
        public static List<Payment> GetBetween(DateTime from, DateTime to)
        {
            List<Payment> list = new List<Payment>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetBetween", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Payment payment = Map(r);
                        payment.ClaimNumber = (string)r["ClaimNumber"];
                        payment.CustomerName = (string)r["CustomerName"];
                        payment.ServiceName = (string)r["ServiceName"];
                        payment.WeightKg = (decimal)r["WeightKg"];
                        payment.JobTotal = (decimal)r["JobTotal"];
                        payment.Reason = r["Reason"] as string;
                        list.Add(payment);
                    }
                }
            }
            return list;
        }

        /// <summary>The columns every payment query returns. The ledger adds its own after this.</summary>
        private static Payment Map(SqlDataReader r)
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
            return payment;
        }
    }
}

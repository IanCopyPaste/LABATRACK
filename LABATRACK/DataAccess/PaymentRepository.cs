using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The Payments table and the vw_JobPayments view. The payment rules (full payment only,
    /// cash received at least the total, refunds on void) are in JobOrderService and StatusService.
    /// </summary>
    public static class PaymentRepository
    {
        public static void Insert(SqlConnection conn, SqlTransaction tx, int jobId, string kind, decimal amount,
                                  string method, decimal? cashReceived, int recordedByUserId)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_Payments_Insert"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                Db.AddText(cmd, "@Kind", kind, 12);
                Db.AddMoney(cmd, "@Amount", amount);
                Db.AddText(cmd, "@Method", method, 10);
                Db.AddMoney(cmd, "@CashReceived", cashReceived);
                Db.AddInt(cmd, "@RecordedByUserId", recordedByUserId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>A job's payments and refunds, oldest first.</summary>
        public static List<Payment> GetByJob(int jobId)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Payments_GetByJob"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                var list = new List<Payment>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(Map(r));
                }
                return list;
            }
        }

        /// <summary>What has been paid on a job, net of refunds, from vw_JobPayments.</summary>
        public static JobPayments GetJobTotals(int jobId)
        {
            using (var conn = Db.Open())
            {
                return GetJobTotals(conn, null, jobId);
            }
        }

        public static JobPayments GetJobTotals(SqlConnection conn, SqlTransaction tx, int jobId)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_Payments_GetJobTotals"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new JobPayments
                    {
                        JobId = Db.GetInt(r, "JobId"),
                        TotalAmount = Db.GetDecimal(r, "TotalAmount"),
                        AmountPaid = Db.GetDecimal(r, "AmountPaid"),
                        CashPaid = Db.GetDecimal(r, "CashPaid"),
                        EwalletPaid = Db.GetDecimal(r, "EwalletPaid"),
                        PaymentCount = Db.GetInt(r, "PaymentCount"),
                    };
                }
            }
        }

        /// <summary>Money in and refunded within [from, to), by method, counted on the payment date.</summary>
        public static DaySales GetDaySummary(DateTime from, DateTime to)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Payments_GetDaySummary"))
            {
                Db.AddDateTime(cmd, "@From", from);
                Db.AddDateTime(cmd, "@To", to);
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    return new DaySales
                    {
                        CashIn = Db.GetDecimal(r, "CashIn"),
                        CashRefunds = Db.GetDecimal(r, "CashRefunds"),
                        EwalletIn = Db.GetDecimal(r, "EwalletIn"),
                        EwalletRefunds = Db.GetDecimal(r, "EwalletRefunds"),
                        PaymentCount = Db.GetInt(r, "PaymentCount"),
                        RefundCount = Db.GetInt(r, "RefundCount"),
                    };
                }
            }
        }

        /// <summary>Every payment and refund within [from, to) with its job: the daily ledger.</summary>
        public static List<Payment> GetBetween(DateTime from, DateTime to)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Payments_GetBetween"))
            {
                Db.AddDateTime(cmd, "@From", from);
                Db.AddDateTime(cmd, "@To", to);
                var list = new List<Payment>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var p = Map(r);
                        p.ClaimNumber = Db.GetText(r, "ClaimNumber");
                        p.CustomerName = Db.GetText(r, "CustomerName");
                        p.ServiceName = Db.GetText(r, "ServiceName");
                        p.WeightKg = Db.GetDecimal(r, "WeightKg");
                        p.Reason = Db.GetText(r, "Reason");
                        list.Add(p);
                    }
                }
                return list;
            }
        }

        private static Payment Map(IDataRecord r)
        {
            return new Payment
            {
                PaymentId = Db.GetInt(r, "PaymentId"),
                JobId = Db.GetInt(r, "JobId"),
                Kind = Db.GetText(r, "Kind"),
                Amount = Db.GetDecimal(r, "Amount"),
                Method = Db.GetText(r, "Method"),
                CashReceived = Db.GetDecimalOrNull(r, "CashReceived"),
                PaidAt = Db.GetDateTime(r, "PaidAt"),
                RecordedByUserId = Db.GetInt(r, "RecordedByUserId"),
                RecordedBy = Db.GetText(r, "RecordedBy"),
            };
        }
    }
}

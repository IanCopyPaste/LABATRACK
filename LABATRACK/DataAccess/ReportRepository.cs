using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The owner's report queries (usp_Reports_...). ReportService adds these rows up into the
    /// figures and charts on Admin/Reports.aspx.
    /// </summary>
    public static class ReportRepository
    {
        /// <summary>When the first job was dropped off, or null if there are no jobs yet.</summary>
        public static DateTime? GetFirstJobDate()
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Reports_GetFirstJobDate", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                object first = cmd.ExecuteScalar();
                if (first == null || first == DBNull.Value) return null;
                return (DateTime)first;
            }
        }

        /// <summary>Every payment and refund within [from, to), by payment date.</summary>
        public static List<ReportPayment> GetPaymentsBetween(DateTime from, DateTime to)
        {
            List<ReportPayment> list = new List<ReportPayment>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Reports_GetPaymentsBetween", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        ReportPayment payment = new ReportPayment();
                        payment.PaidAt = (DateTime)r["PaidAt"];
                        payment.Amount = (decimal)r["Amount"];
                        payment.Method = (string)r["Method"];
                        payment.Kind = (string)r["Kind"];
                        list.Add(payment);
                    }
                }
            }
            return list;
        }

        /// <summary>Every job dropped off within [from, to), with its add-on lines attached.</summary>
        public static List<ReportJob> GetJobsBetween(DateTime from, DateTime to)
        {
            List<ReportJob> jobs = new List<ReportJob>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();

                // 1. The jobs.
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Reports_GetJobsBetween", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                    cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            ReportJob job = new ReportJob();
                            job.JobId = (int)r["JobId"];
                            job.ClaimNumber = (string)r["ClaimNumber"];
                            job.CreatedAt = (DateTime)r["CreatedAt"];
                            job.ServiceId = (int)r["ServiceId"];
                            job.ServiceName = (string)r["ServiceName"];
                            job.WeightKg = (decimal)r["WeightKg"];
                            job.BillableKg = (decimal)r["BillableKg"];
                            job.RatePerKgAtCreation = (decimal)r["RatePerKgAtCreation"];
                            job.MinimumChargeAtCreation = (decimal)r["MinimumChargeAtCreation"];
                            job.LaundryCharge = (decimal)r["LaundryCharge"];
                            job.TotalAmount = (decimal)r["TotalAmount"];
                            job.CurrentStatus = (string)r["CurrentStatus"];
                            job.CustomerId = (int)r["CustomerId"];
                            job.CustomerName = (string)r["CustomerName"];
                            job.CreatedByName = (string)r["CreatedByName"];
                            job.FirstReadyAt = r["FirstReadyAt"] as DateTime?;
                            job.IsReturning = Convert.ToBoolean(r["IsReturning"]);
                            job.CustomerFirstJobAt = r["CustomerFirstJobAt"] as DateTime?;
                            job.VoidReason = r["VoidReason"] as string;
                            job.AmountRefunded = (decimal)r["AmountRefunded"];
                            job.RefundPending = (decimal)r["RefundPending"];
                            jobs.Add(job);
                        }
                    }
                }

                // 2. The add-on lines of those jobs, each attached to its job.
                Dictionary<int, ReportJob> byJob = jobs.ToDictionary(j => j.JobId);
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Reports_GetAddOnLinesBetween", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                    cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            ReportJob job;
                            if (!byJob.TryGetValue((int)r["JobId"], out job)) continue;

                            ReportAddOnLine line = new ReportAddOnLine();
                            line.JobId = job.JobId;
                            line.AddOnId = (int)r["AddOnId"];
                            line.Name = (string)r["Name"];
                            line.Kind = (string)r["Kind"];
                            line.Quantity = (int)r["Quantity"];
                            line.PriceAtCreation = (decimal)r["PriceAtCreation"];
                            job.AddOns.Add(line);
                        }
                    }
                }
            }
            return jobs;
        }

        /// <summary>Sales and job orders for every year there is data.</summary>
        public static List<YearTotal> GetYearTotals()
        {
            List<YearTotal> list = new List<YearTotal>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Reports_GetYearTotals", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        YearTotal total = new YearTotal();
                        total.Year = Convert.ToInt32(r["SalesYear"]);
                        total.Sales = Convert.ToDecimal(r["Sales"]);
                        total.Orders = Convert.ToInt32(r["Orders"]);
                        list.Add(total);
                    }
                }
            }
            return list;
        }
    }
}

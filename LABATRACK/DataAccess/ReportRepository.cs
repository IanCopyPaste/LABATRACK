using System;
using System.Collections.Generic;
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
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Reports_GetFirstJobDate"))
            {
                object v = cmd.ExecuteScalar();
                return v == null || v == DBNull.Value ? (DateTime?)null : (DateTime)v;
            }
        }

        /// <summary>Every payment and refund within [from, to), by payment date.</summary>
        public static List<ReportPayment> GetPaymentsBetween(DateTime from, DateTime to)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Reports_GetPaymentsBetween"))
            {
                Db.AddDateTime(cmd, "@From", from);
                Db.AddDateTime(cmd, "@To", to);
                var list = new List<ReportPayment>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ReportPayment
                        {
                            PaidAt = Db.GetDateTime(r, "PaidAt"),
                            Amount = Db.GetDecimal(r, "Amount"),
                            Method = Db.GetText(r, "Method"),
                            Kind = Db.GetText(r, "Kind"),
                        });
                    }
                }
                return list;
            }
        }

        /// <summary>Every job dropped off within [from, to), with its add-on lines attached.</summary>
        public static List<ReportJob> GetJobsBetween(DateTime from, DateTime to)
        {
            var jobs = new List<ReportJob>();
            using (var conn = Db.Open())
            {
                using (var cmd = Db.Proc(conn, null, "dbo.usp_Reports_GetJobsBetween"))
                {
                    Db.AddDateTime(cmd, "@From", from);
                    Db.AddDateTime(cmd, "@To", to);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            jobs.Add(new ReportJob
                            {
                                JobId = Db.GetInt(r, "JobId"),
                                ClaimNumber = Db.GetText(r, "ClaimNumber"),
                                CreatedAt = Db.GetDateTime(r, "CreatedAt"),
                                ServiceId = Db.GetInt(r, "ServiceId"),
                                ServiceName = Db.GetText(r, "ServiceName"),
                                WeightKg = Db.GetDecimal(r, "WeightKg"),
                                BillableKg = Db.GetDecimal(r, "BillableKg"),
                                RatePerKgAtCreation = Db.GetDecimal(r, "RatePerKgAtCreation"),
                                MinimumChargeAtCreation = Db.GetDecimal(r, "MinimumChargeAtCreation"),
                                LaundryCharge = Db.GetDecimal(r, "LaundryCharge"),
                                TotalAmount = Db.GetDecimal(r, "TotalAmount"),
                                CurrentStatus = Db.GetText(r, "CurrentStatus"),
                                CustomerId = Db.GetInt(r, "CustomerId"),
                                CustomerName = Db.GetText(r, "CustomerName"),
                                CreatedByName = Db.GetText(r, "CreatedByName"),
                                FirstReadyAt = Db.GetDateTimeOrNull(r, "FirstReadyAt"),
                                IsReturning = Db.GetBool(r, "IsReturning"),
                                CustomerFirstJobAt = Db.GetDateTimeOrNull(r, "CustomerFirstJobAt"),
                                VoidReason = Db.GetText(r, "VoidReason"),
                            });
                        }
                    }
                }

                using (var cmd = Db.Proc(conn, null, "dbo.usp_Reports_GetAddOnLinesBetween"))
                {
                    Db.AddDateTime(cmd, "@From", from);
                    Db.AddDateTime(cmd, "@To", to);
                    var byJob = jobs.ToDictionary(j => j.JobId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            ReportJob job;
                            if (!byJob.TryGetValue(Db.GetInt(r, "JobId"), out job)) continue;
                            job.AddOns.Add(new ReportAddOnLine
                            {
                                JobId = job.JobId,
                                AddOnId = Db.GetInt(r, "AddOnId"),
                                Name = Db.GetText(r, "Name"),
                                Kind = Db.GetText(r, "Kind"),
                                Quantity = Db.GetInt(r, "Quantity"),
                                PriceAtCreation = Db.GetDecimal(r, "PriceAtCreation"),
                            });
                        }
                    }
                }
            }
            return jobs;
        }

        /// <summary>Sales and job orders for every year there is data.</summary>
        public static List<YearTotal> GetYearTotals()
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Reports_GetYearTotals"))
            {
                var list = new List<YearTotal>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new YearTotal
                        {
                            Year = Db.GetInt(r, "SalesYear"),
                            Sales = Db.GetDecimal(r, "Sales"),
                            Orders = Db.GetInt(r, "Orders"),
                        });
                    }
                }
                return list;
            }
        }
    }
}

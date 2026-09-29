using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The JobOrders and JobAddOns tables, through their stored procedures. Every job query
    /// returns rows of the vw_JobList view, so a job is read the same way everywhere.
    /// Methods that take a connection and a transaction are steps of a larger save and must run
    /// inside it; the others open their own connection.
    /// </summary>
    public static class JobOrderRepository
    {
        // ------------------------------------------------------------ Reading jobs

        public static JobOrder GetById(int jobId)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_JobOrders_GetById"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                return ReadOne(cmd);
            }
        }

        /// <summary>
        /// Reads the job inside a transaction and locks its row until the transaction ends, so
        /// no one else can change it while this save decides what to do.
        /// </summary>
        public static JobOrder GetForUpdate(SqlConnection conn, SqlTransaction tx, int jobId)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobOrders_GetForUpdate"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                return ReadOne(cmd);
            }
        }

        /// <summary>
        /// The job board: every job from Queued to Ready for pick-up, oldest first. The staff board
        /// and the owner's dashboard both use this one method.
        /// </summary>
        public static List<JobOrder> GetActive()
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_JobOrders_GetActive"))
            {
                return ReadAll(cmd);
            }
        }

        /// <summary>Jobs that became Claimed (or Voided) within [from, to), newest first.</summary>
        public static List<JobOrder> GetClosedBetween(string status, DateTime from, DateTime to)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_JobOrders_GetClosedBetween"))
            {
                Db.AddText(cmd, "@Status", status, 20);
                Db.AddDateTime(cmd, "@From", from);
                Db.AddDateTime(cmd, "@To", to);
                return ReadAll(cmd);
            }
        }

        /// <summary>Jobs Ready for pick-up since before <paramref name="readyBefore"/>, longest waiting first.</summary>
        public static List<JobOrder> GetReadyBefore(DateTime readyBefore)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_JobOrders_GetReadyBefore"))
            {
                Db.AddDateTime(cmd, "@ReadyBefore", readyBefore);
                return ReadAll(cmd);
            }
        }

        /// <summary>Every job dropped off within [from, to), voided ones included, oldest first.</summary>
        public static List<JobOrder> GetCreatedBetween(DateTime from, DateTime to)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_JobOrders_GetCreatedBetween"))
            {
                Db.AddDateTime(cmd, "@From", from);
                Db.AddDateTime(cmd, "@To", to);
                return ReadAll(cmd);
            }
        }

        /// <summary>One page of the History search, newest drop-off first (see usp_JobOrders_Search).</summary>
        public static List<JobOrder> Search(string text, string digits, string status, DateTime? from, DateTime? to,
                                            bool includeVoided, int offset, int pageSize)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_JobOrders_Search"))
            {
                AddSearchFilters(cmd, text, digits, status, from, to, includeVoided);
                Db.AddInt(cmd, "@Offset", offset);
                Db.AddInt(cmd, "@PageSize", pageSize);
                return ReadAll(cmd);
            }
        }

        /// <summary>How many jobs the History search finds, and their sales (voided jobs not counted).</summary>
        public static void SearchSummary(string text, string digits, string status, DateTime? from, DateTime? to,
                                         bool includeVoided, out int jobCount, out decimal salesTotal)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_JobOrders_SearchSummary"))
            {
                AddSearchFilters(cmd, text, digits, status, from, to, includeVoided);
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    jobCount = Db.GetInt(r, "JobCount");
                    salesTotal = Db.GetDecimal(r, "SalesTotal");
                }
            }
        }

        private static void AddSearchFilters(SqlCommand cmd, string text, string digits, string status,
                                             DateTime? from, DateTime? to, bool includeVoided)
        {
            Db.AddText(cmd, "@Text", text, 100);
            Db.AddText(cmd, "@Digits", digits, 20);
            Db.AddText(cmd, "@Status", status, 20);
            Db.AddDateTime(cmd, "@From", from);
            Db.AddDateTime(cmd, "@To", to);
            Db.AddBit(cmd, "@IncludeVoided", includeVoided);
        }

        // ------------------------------------------------------------ Saving jobs

        /// <summary>The day's latest claim number, locking that day's numbers until the transaction ends.</summary>
        public static string GetLastClaimNumber(SqlConnection conn, SqlTransaction tx, string prefix)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobOrders_GetLastClaimNumber"))
            {
                Db.AddText(cmd, "@Prefix", prefix, 12);
                object v = cmd.ExecuteScalar();
                return v == null || v == DBNull.Value ? null : (string)v;
            }
        }

        public static int Insert(SqlConnection conn, SqlTransaction tx, JobOrder job)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobOrders_Insert"))
            {
                Db.AddText(cmd, "@ClaimNumber", job.ClaimNumber, 20);
                Db.AddInt(cmd, "@CustomerId", job.CustomerId);
                Db.AddInt(cmd, "@ServiceId", job.ServiceId);
                Db.AddKg(cmd, "@WeightKg", job.WeightKg);
                Db.AddKg(cmd, "@BillableKg", job.BillableKg);
                Db.AddMoney(cmd, "@RatePerKgAtCreation", job.RatePerKgAtCreation);
                Db.AddMoney(cmd, "@MinimumChargeAtCreation", job.MinimumChargeAtCreation);
                Db.AddDecimal(cmd, "@RoundingIncrementKgAtCreation", job.RoundingIncrementKgAtCreation, 4, 2);
                Db.AddMoney(cmd, "@LaundryCharge", job.LaundryCharge);
                Db.AddBit(cmd, "@IncludesWashing", job.IncludesWashing);
                Db.AddBit(cmd, "@IncludesDrying", job.IncludesDrying);
                Db.AddBit(cmd, "@IncludesFolding", job.IncludesFolding);
                Db.AddMoney(cmd, "@TotalAmount", job.TotalAmount);
                Db.AddText(cmd, "@CurrentStatus", job.CurrentStatus, 20);
                Db.AddText(cmd, "@Remarks", job.Remarks, 500);
                Db.AddDateTime(cmd, "@ExpectedPickupAt", job.ExpectedPickupAt);
                Db.AddInt(cmd, "@CreatedByUserId", job.CreatedByUserId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Moves the job to <paramref name="newStatus"/> only if it is still in
        /// <paramref name="expectedStatus"/>. Returns false if someone else changed it first.
        /// </summary>
        public static bool UpdateStatus(SqlConnection conn, SqlTransaction tx, int jobId, string expectedStatus, string newStatus)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobOrders_UpdateStatus"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                Db.AddText(cmd, "@ExpectedStatus", expectedStatus, 20);
                Db.AddText(cmd, "@NewStatus", newStatus, 20);
                return Db.RowsAffected(cmd) == 1;
            }
        }

        /// <summary>
        /// Saves the owner's correction of a job, only if it is still in <paramref name="expectedStatus"/>.
        /// Returns false if the job moved while the owner was editing it.
        /// </summary>
        public static bool Update(SqlConnection conn, SqlTransaction tx, JobOrder job, string expectedStatus, int editedByUserId)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobOrders_Update"))
            {
                Db.AddInt(cmd, "@JobId", job.JobId);
                Db.AddText(cmd, "@ExpectedStatus", expectedStatus, 20);
                Db.AddInt(cmd, "@ServiceId", job.ServiceId);
                Db.AddKg(cmd, "@WeightKg", job.WeightKg);
                Db.AddKg(cmd, "@BillableKg", job.BillableKg);
                Db.AddMoney(cmd, "@RatePerKgAtCreation", job.RatePerKgAtCreation);
                Db.AddMoney(cmd, "@LaundryCharge", job.LaundryCharge);
                Db.AddBit(cmd, "@IncludesWashing", job.IncludesWashing);
                Db.AddBit(cmd, "@IncludesDrying", job.IncludesDrying);
                Db.AddBit(cmd, "@IncludesFolding", job.IncludesFolding);
                Db.AddMoney(cmd, "@TotalAmount", job.TotalAmount);
                Db.AddText(cmd, "@Remarks", job.Remarks, 500);
                Db.AddInt(cmd, "@EditedByUserId", editedByUserId);
                return Db.RowsAffected(cmd) == 1;
            }
        }

        // ------------------------------------------------------------ Add-on lines

        /// <summary>The add-ons currently on a job, services first.</summary>
        public static List<JobAddOn> GetAddOns(int jobId)
        {
            using (var conn = Db.Open())
            {
                return GetAddOns(conn, null, jobId);
            }
        }

        public static List<JobAddOn> GetAddOns(SqlConnection conn, SqlTransaction tx, int jobId)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobAddOns_GetByJob"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                var list = new List<JobAddOn>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new JobAddOn
                        {
                            JobAddOnId = Db.GetInt(r, "JobAddOnId"),
                            JobId = Db.GetInt(r, "JobId"),
                            AddOnId = Db.GetInt(r, "AddOnId"),
                            Name = Db.GetText(r, "Name"),
                            Kind = Db.GetText(r, "Kind"),
                            Quantity = Db.GetInt(r, "Quantity"),
                            PriceAtCreation = Db.GetDecimal(r, "PriceAtCreation"),
                        });
                    }
                }
                return list;
            }
        }

        public static void InsertAddOn(SqlConnection conn, SqlTransaction tx, int jobId, int addOnId, int quantity, decimal priceAtCreation)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobAddOns_Insert"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                Db.AddInt(cmd, "@AddOnId", addOnId);
                Db.AddInt(cmd, "@Quantity", quantity);
                Db.AddMoney(cmd, "@PriceAtCreation", priceAtCreation);
                cmd.ExecuteNonQuery();
            }
        }

        public static void SetAddOnQuantity(SqlConnection conn, SqlTransaction tx, int jobAddOnId, int quantity)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobAddOns_SetQuantity"))
            {
                Db.AddInt(cmd, "@JobAddOnId", jobAddOnId);
                Db.AddInt(cmd, "@Quantity", quantity);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Takes an add-on off a job. The line is marked removed, never deleted.</summary>
        public static void RemoveAddOn(SqlConnection conn, SqlTransaction tx, int jobAddOnId)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobAddOns_Remove"))
            {
                Db.AddInt(cmd, "@JobAddOnId", jobAddOnId);
                cmd.ExecuteNonQuery();
            }
        }

        // ------------------------------------------------------------ Mapping

        private static JobOrder ReadOne(SqlCommand cmd)
        {
            using (var r = cmd.ExecuteReader())
            {
                return r.Read() ? Map(r) : null;
            }
        }

        private static List<JobOrder> ReadAll(SqlCommand cmd)
        {
            var list = new List<JobOrder>();
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read()) list.Add(Map(r));
            }
            return list;
        }

        /// <summary>One row of vw_JobList.</summary>
        private static JobOrder Map(IDataRecord r)
        {
            return new JobOrder
            {
                JobId = Db.GetInt(r, "JobId"),
                ClaimNumber = Db.GetText(r, "ClaimNumber"),
                CustomerId = Db.GetInt(r, "CustomerId"),
                CustomerName = Db.GetText(r, "CustomerName"),
                ContactNumber = Db.GetText(r, "ContactNumber"),
                CustomerEmail = Db.GetText(r, "CustomerEmail"),
                ServiceId = Db.GetInt(r, "ServiceId"),
                ServiceName = Db.GetText(r, "ServiceName"),
                WeightKg = Db.GetDecimal(r, "WeightKg"),
                BillableKg = Db.GetDecimal(r, "BillableKg"),
                RatePerKgAtCreation = Db.GetDecimal(r, "RatePerKgAtCreation"),
                MinimumChargeAtCreation = Db.GetDecimal(r, "MinimumChargeAtCreation"),
                RoundingIncrementKgAtCreation = Db.GetDecimal(r, "RoundingIncrementKgAtCreation"),
                LaundryCharge = Db.GetDecimal(r, "LaundryCharge"),
                TotalAmount = Db.GetDecimal(r, "TotalAmount"),
                IncludesWashing = Db.GetBool(r, "IncludesWashing"),
                IncludesDrying = Db.GetBool(r, "IncludesDrying"),
                IncludesFolding = Db.GetBool(r, "IncludesFolding"),
                CurrentStatus = Db.GetText(r, "CurrentStatus"),
                Remarks = Db.GetText(r, "Remarks"),
                ExpectedPickupAt = Db.GetDateTime(r, "ExpectedPickupAt"),
                CreatedAt = Db.GetDateTime(r, "CreatedAt"),
                CreatedByUserId = Db.GetInt(r, "CreatedByUserId"),
                CreatedByName = Db.GetText(r, "CreatedByName"),
                LastEditedAt = Db.GetDateTimeOrNull(r, "LastEditedAt"),
                LastEditedByName = Db.GetText(r, "LastEditedByName"),
                StatusSince = Db.GetDateTimeOrNull(r, "StatusSince") ?? Db.GetDateTime(r, "CreatedAt"),
                StatusBy = Db.GetText(r, "StatusBy"),
                StatusFrom = Db.GetText(r, "StatusFrom"),
                StatusReason = Db.GetText(r, "StatusReason"),
                AmountPaid = Db.GetDecimal(r, "AmountPaid"),
                PaymentMethod = Db.GetText(r, "PaymentMethod"),
                ServiceAddOnAmount = Db.GetDecimal(r, "ServiceAddOnAmount"),
                ProductAddOnAmount = Db.GetDecimal(r, "ProductAddOnAmount"),
                AddOnSummary = Db.GetText(r, "AddOnSummary") ?? "",
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The JobOrders and JobAddOns tables, through their stored procedures. Every job query
    /// returns rows of the vw_JobList view, so a job is read the same way everywhere (see Map at
    /// the bottom). Methods that take a connection and a transaction are steps of a larger save
    /// and must run inside it; the others open their own connection.
    /// </summary>
    public static class JobOrderRepository
    {
        // ------------------------------------------------------------ Reading jobs

        public static JobOrder GetById(int jobId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return Map(r);
                }
            }
        }

        /// <summary>
        /// Reads the job inside a transaction and locks its row until the transaction ends, so
        /// no one else can change it while this save decides what to do. The connection and
        /// transaction come from the caller.
        /// </summary>
        public static JobOrder GetForUpdate(SqlConnection conn, SqlTransaction tx, int jobId)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetForUpdate", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return Map(r);
                }
            }
        }

        /// <summary>
        /// The job board: every job from Queued to Ready for pick-up, oldest first. The staff board
        /// and the owner's dashboard both use this one method.
        /// </summary>
        public static List<JobOrder> GetActive()
        {
            List<JobOrder> list = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetActive", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

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

        /// <summary>Jobs that became Claimed (or Voided) within [from, to), newest first.</summary>
        public static List<JobOrder> GetClosedBetween(string status, DateTime from, DateTime to)
        {
            List<JobOrder> list = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetClosedBetween", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = status;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

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

        /// <summary>Voided jobs whose refund has not been handed over yet, longest waiting first.</summary>
        public static List<JobOrder> GetPendingRefunds()
        {
            List<JobOrder> list = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetPendingRefunds", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

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

        /// <summary>Jobs Ready for pick-up since before <paramref name="readyBefore"/>, longest waiting first.</summary>
        public static List<JobOrder> GetReadyBefore(DateTime readyBefore)
        {
            List<JobOrder> list = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetReadyBefore", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ReadyBefore", SqlDbType.DateTime2).Value = readyBefore;

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

        /// <summary>Every job dropped off within [from, to), voided ones included, oldest first.</summary>
        public static List<JobOrder> GetCreatedBetween(DateTime from, DateTime to)
        {
            List<JobOrder> list = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetCreatedBetween", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to;

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

        /// <summary>One page of the History search, newest drop-off first (see usp_JobOrders_Search).</summary>
        public static List<JobOrder> Search(string text, string digits, string status, DateTime? from, DateTime? to,
                                            bool includeVoided, int offset, int pageSize)
        {
            List<JobOrder> list = new List<JobOrder>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_Search", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 100).Value = (object)text ?? DBNull.Value;
                cmd.Parameters.Add("@Digits", SqlDbType.NVarChar, 20).Value = (object)digits ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = (object)status ?? DBNull.Value;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = (object)from ?? DBNull.Value;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = (object)to ?? DBNull.Value;
                cmd.Parameters.Add("@IncludeVoided", SqlDbType.Bit).Value = includeVoided;
                cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = offset;
                cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

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

        /// <summary>How many jobs the History search finds, and their sales (voided jobs not counted).</summary>
        public static void SearchSummary(string text, string digits, string status, DateTime? from, DateTime? to,
                                         bool includeVoided, out int jobCount, out decimal salesTotal)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_SearchSummary", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 100).Value = (object)text ?? DBNull.Value;
                cmd.Parameters.Add("@Digits", SqlDbType.NVarChar, 20).Value = (object)digits ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = (object)status ?? DBNull.Value;
                cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = (object)from ?? DBNull.Value;
                cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = (object)to ?? DBNull.Value;
                cmd.Parameters.Add("@IncludeVoided", SqlDbType.Bit).Value = includeVoided;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    r.Read();   // the procedure always returns exactly one row
                    jobCount = Convert.ToInt32(r["JobCount"]);
                    salesTotal = Convert.ToDecimal(r["SalesTotal"]);
                }
            }
        }

        // ------------------------------------------------------------ Saving jobs
        // The methods below are steps of a larger save: the connection and the transaction come
        // from the caller (JobOrderService, StatusService), so all the steps succeed or none do.

        /// <summary>The day's latest claim number, locking that day's numbers until the transaction ends.</summary>
        public static string GetLastClaimNumber(SqlConnection conn, SqlTransaction tx, string prefix)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetLastClaimNumber", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Prefix", SqlDbType.NVarChar, 12).Value = prefix;

                object last = cmd.ExecuteScalar();
                if (last == null || last == DBNull.Value) return null;
                return (string)last;
            }
        }

        public static int Insert(SqlConnection conn, SqlTransaction tx, JobOrder job)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_Insert", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ClaimNumber", SqlDbType.NVarChar, 20).Value = job.ClaimNumber;
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = job.CustomerId;
                cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = job.ServiceId;
                cmd.Parameters.Add(new SqlParameter("@WeightKg", SqlDbType.Decimal) { Precision = 6, Scale = 2, Value = job.WeightKg });
                cmd.Parameters.Add(new SqlParameter("@BillableKg", SqlDbType.Decimal) { Precision = 6, Scale = 2, Value = job.BillableKg });
                cmd.Parameters.Add(new SqlParameter("@RatePerKgAtCreation", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = job.RatePerKgAtCreation });
                cmd.Parameters.Add(new SqlParameter("@MinimumChargeAtCreation", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = job.MinimumChargeAtCreation });
                cmd.Parameters.Add(new SqlParameter("@RoundingIncrementKgAtCreation", SqlDbType.Decimal) { Precision = 4, Scale = 2, Value = job.RoundingIncrementKgAtCreation });
                cmd.Parameters.Add(new SqlParameter("@LaundryCharge", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = job.LaundryCharge });
                cmd.Parameters.Add("@IncludesWashing", SqlDbType.Bit).Value = job.IncludesWashing;
                cmd.Parameters.Add("@IncludesDrying", SqlDbType.Bit).Value = job.IncludesDrying;
                cmd.Parameters.Add("@IncludesFolding", SqlDbType.Bit).Value = job.IncludesFolding;
                cmd.Parameters.Add(new SqlParameter("@TotalAmount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = job.TotalAmount });
                cmd.Parameters.Add("@CurrentStatus", SqlDbType.NVarChar, 20).Value = job.CurrentStatus;
                cmd.Parameters.Add("@Remarks", SqlDbType.NVarChar, 500).Value = (object)job.Remarks ?? DBNull.Value;
                cmd.Parameters.Add("@ExpectedPickupAt", SqlDbType.DateTime2).Value = job.ExpectedPickupAt;
                cmd.Parameters.Add("@CreatedByUserId", SqlDbType.Int).Value = job.CreatedByUserId;

                return Convert.ToInt32(cmd.ExecuteScalar());   // the new JobId
            }
        }

        /// <summary>
        /// Moves the job to <paramref name="newStatus"/> only if it is still in
        /// <paramref name="expectedStatus"/>. Returns false if someone else changed it first.
        /// </summary>
        public static bool UpdateStatus(SqlConnection conn, SqlTransaction tx, int jobId, string expectedStatus, string newStatus)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_UpdateStatus", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@ExpectedStatus", SqlDbType.NVarChar, 20).Value = expectedStatus;
                cmd.Parameters.Add("@NewStatus", SqlDbType.NVarChar, 20).Value = newStatus;

                // The procedure ends with SELECT @@ROWCOUNT: 1 = moved, 0 = someone else moved it first.
                int rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                return rowsAffected == 1;
            }
        }

        /// <summary>
        /// Saves the owner's correction of a job, only if it is still in <paramref name="expectedStatus"/>.
        /// Returns false if the job moved while the owner was editing it.
        /// </summary>
        public static bool Update(SqlConnection conn, SqlTransaction tx, JobOrder job, string expectedStatus, int editedByUserId)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_Update", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = job.JobId;
                cmd.Parameters.Add("@ExpectedStatus", SqlDbType.NVarChar, 20).Value = expectedStatus;
                cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = job.ServiceId;
                cmd.Parameters.Add(new SqlParameter("@WeightKg", SqlDbType.Decimal) { Precision = 6, Scale = 2, Value = job.WeightKg });
                cmd.Parameters.Add(new SqlParameter("@BillableKg", SqlDbType.Decimal) { Precision = 6, Scale = 2, Value = job.BillableKg });
                cmd.Parameters.Add(new SqlParameter("@RatePerKgAtCreation", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = job.RatePerKgAtCreation });
                cmd.Parameters.Add(new SqlParameter("@LaundryCharge", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = job.LaundryCharge });
                cmd.Parameters.Add("@IncludesWashing", SqlDbType.Bit).Value = job.IncludesWashing;
                cmd.Parameters.Add("@IncludesDrying", SqlDbType.Bit).Value = job.IncludesDrying;
                cmd.Parameters.Add("@IncludesFolding", SqlDbType.Bit).Value = job.IncludesFolding;
                cmd.Parameters.Add(new SqlParameter("@TotalAmount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = job.TotalAmount });
                cmd.Parameters.Add("@Remarks", SqlDbType.NVarChar, 500).Value = (object)job.Remarks ?? DBNull.Value;
                cmd.Parameters.Add("@EditedByUserId", SqlDbType.Int).Value = editedByUserId;

                int rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                return rowsAffected == 1;
            }
        }

        // ------------------------------------------------------------ Add-on lines

        /// <summary>The add-ons currently on a job, services first.</summary>
        public static List<JobAddOn> GetAddOns(int jobId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                return GetAddOns(conn, null, jobId);
            }
        }

        /// <summary>The same, read inside a save's transaction (the connection and transaction come from the caller).</summary>
        public static List<JobAddOn> GetAddOns(SqlConnection conn, SqlTransaction tx, int jobId)
        {
            List<JobAddOn> list = new List<JobAddOn>();
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_GetByJob", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

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
                        list.Add(line);
                    }
                }
            }
            return list;
        }

        public static void InsertAddOn(SqlConnection conn, SqlTransaction tx, int jobId, int addOnId, int quantity, decimal priceAtCreation)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_Insert", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@AddOnId", SqlDbType.Int).Value = addOnId;
                cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity;
                cmd.Parameters.Add(new SqlParameter("@PriceAtCreation", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = priceAtCreation });

                cmd.ExecuteNonQuery();
            }
        }

        public static void SetAddOnQuantity(SqlConnection conn, SqlTransaction tx, int jobAddOnId, int quantity)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_SetQuantity", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobAddOnId", SqlDbType.Int).Value = jobAddOnId;
                cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity;

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Takes an add-on off a job. The line is marked removed, never deleted.</summary>
        public static void RemoveAddOn(SqlConnection conn, SqlTransaction tx, int jobAddOnId)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_Remove", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobAddOnId", SqlDbType.Int).Value = jobAddOnId;

                cmd.ExecuteNonQuery();
            }
        }

        // ------------------------------------------------------------ Reading one row

        /// <summary>
        /// One row of vw_JobList. A column that can be empty in SQL is read with "as"; the two
        /// add-on amounts come from ISNULL(SUM(...)) expressions, so they are converted.
        /// </summary>
        private static JobOrder Map(SqlDataReader r)
        {
            JobOrder job = new JobOrder();
            job.JobId = (int)r["JobId"];
            job.ClaimNumber = (string)r["ClaimNumber"];
            job.CustomerId = (int)r["CustomerId"];
            job.CustomerName = (string)r["CustomerName"];
            job.ContactNumber = (string)r["ContactNumber"];
            job.CustomerEmail = r["CustomerEmail"] as string;
            job.ServiceId = (int)r["ServiceId"];
            job.ServiceName = (string)r["ServiceName"];
            job.WeightKg = (decimal)r["WeightKg"];
            job.BillableKg = (decimal)r["BillableKg"];
            job.RatePerKgAtCreation = (decimal)r["RatePerKgAtCreation"];
            job.MinimumChargeAtCreation = (decimal)r["MinimumChargeAtCreation"];
            job.RoundingIncrementKgAtCreation = (decimal)r["RoundingIncrementKgAtCreation"];
            job.LaundryCharge = (decimal)r["LaundryCharge"];
            job.TotalAmount = (decimal)r["TotalAmount"];
            job.IncludesWashing = (bool)r["IncludesWashing"];
            job.IncludesDrying = (bool)r["IncludesDrying"];
            job.IncludesFolding = (bool)r["IncludesFolding"];
            job.CurrentStatus = (string)r["CurrentStatus"];
            job.Remarks = r["Remarks"] as string;
            job.ExpectedPickupAt = (DateTime)r["ExpectedPickupAt"];
            job.CreatedAt = (DateTime)r["CreatedAt"];
            job.CreatedByUserId = (int)r["CreatedByUserId"];
            job.CreatedByName = (string)r["CreatedByName"];
            job.LastEditedAt = r["LastEditedAt"] as DateTime?;
            job.LastEditedByName = r["LastEditedByName"] as string;
            job.StatusSince = (r["StatusSince"] as DateTime?) ?? job.CreatedAt;   // a job always has a first status change
            job.StatusBy = r["StatusBy"] as string;
            job.StatusFrom = r["StatusFrom"] as string;
            job.StatusReason = r["StatusReason"] as string;
            job.AmountPaid = (decimal)r["AmountPaid"];
            job.PaymentMethod = r["PaymentMethod"] as string;
            job.RefundStatus = r["RefundStatus"] as string;
            job.RefundAmount = (r["RefundAmount"] as decimal?) ?? 0m;
            job.RefundReleasedAt = r["RefundReleasedAt"] as DateTime?;
            job.RefundReleasedBy = r["RefundReleasedBy"] as string;
            job.RefundReleasedHow = r["RefundReleasedHow"] as string;
            job.RefundFailedAttempts = (r["RefundFailedAttempts"] as int?) ?? 0;
            job.ServiceAddOnAmount = Convert.ToDecimal(r["ServiceAddOnAmount"]);
            job.ProductAddOnAmount = Convert.ToDecimal(r["ProductAddOnAmount"]);
            job.AddOnSummary = (r["AddOnSummary"] as string) ?? "";
            return job;
        }
    }
}

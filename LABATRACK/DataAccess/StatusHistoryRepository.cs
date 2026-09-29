using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The JobStatusHistory table. Rows are only ever added: a status change is never edited or
    /// deleted, so the timeline of a job is always the true story, rework included.
    /// </summary>
    public static class StatusHistoryRepository
    {
        /// <summary>Adds one history row as a step of a larger save (the connection and transaction come from the caller).</summary>
        public static void Insert(SqlConnection conn, SqlTransaction tx, int jobId, string fromStatus, string toStatus,
                                  int changedByUserId, string reason)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobStatusHistory_Insert", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@FromStatus", SqlDbType.NVarChar, 20).Value = (object)fromStatus ?? DBNull.Value;
                cmd.Parameters.Add("@ToStatus", SqlDbType.NVarChar, 20).Value = toStatus;
                cmd.Parameters.Add("@ChangedByUserId", SqlDbType.Int).Value = changedByUserId;
                cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = (object)reason ?? DBNull.Value;

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>A job's status changes, oldest first.</summary>
        public static List<StatusChange> GetByJob(int jobId)
        {
            List<StatusChange> list = new List<StatusChange>();
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
                        list.Add(change);
                    }
                }
            }
            return list;
        }
    }
}

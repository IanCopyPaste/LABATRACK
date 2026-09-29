using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>The NotificationLog table: one row for every email attempt to a customer.</summary>
    public static class NotificationRepository
    {
        public static void Insert(int jobId, string kind, string recipient, string status, string detail,
                                  bool isResend, int attemptedByUserId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_NotificationLog_Insert", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@Kind", SqlDbType.NVarChar, 10).Value = kind;
                cmd.Parameters.Add("@Recipient", SqlDbType.NVarChar, 254).Value = (object)recipient ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 10).Value = status;
                cmd.Parameters.Add("@Detail", SqlDbType.NVarChar, 400).Value = (object)detail ?? DBNull.Value;
                cmd.Parameters.Add("@IsResend", SqlDbType.Bit).Value = isResend;
                cmd.Parameters.Add("@AttemptedByUserId", SqlDbType.Int).Value = attemptedByUserId;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>A job's email attempts, oldest first.</summary>
        public static List<NotificationLogEntry> GetByJob(int jobId)
        {
            List<NotificationLogEntry> list = new List<NotificationLogEntry>();
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
                        list.Add(entry);
                    }
                }
            }
            return list;
        }
    }
}

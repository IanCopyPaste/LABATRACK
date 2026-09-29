using System.Collections.Generic;
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
        public static void Insert(SqlConnection conn, SqlTransaction tx, int jobId, string fromStatus, string toStatus,
                                  int changedByUserId, string reason)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_JobStatusHistory_Insert"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                Db.AddText(cmd, "@FromStatus", fromStatus, 20);
                Db.AddText(cmd, "@ToStatus", toStatus, 20);
                Db.AddInt(cmd, "@ChangedByUserId", changedByUserId);
                Db.AddText(cmd, "@Reason", reason, 200);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>A job's status changes, oldest first.</summary>
        public static List<StatusChange> GetByJob(int jobId)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_JobStatusHistory_GetByJob"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                var list = new List<StatusChange>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new StatusChange
                        {
                            HistoryId = Db.GetInt(r, "HistoryId"),
                            JobId = Db.GetInt(r, "JobId"),
                            FromStatus = Db.GetText(r, "FromStatus"),
                            ToStatus = Db.GetText(r, "ToStatus"),
                            ChangedAt = Db.GetDateTime(r, "ChangedAt"),
                            ChangedByUserId = Db.GetInt(r, "ChangedByUserId"),
                            ChangedBy = Db.GetText(r, "ChangedBy"),
                            Reason = Db.GetText(r, "Reason"),
                        });
                    }
                }
                return list;
            }
        }
    }
}

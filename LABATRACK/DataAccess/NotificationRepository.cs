using System.Collections.Generic;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>The NotificationLog table: one row for every email attempt to a customer.</summary>
    public static class NotificationRepository
    {
        public static void Insert(int jobId, string kind, string recipient, string status, string detail,
                                  bool isResend, int attemptedByUserId)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_NotificationLog_Insert"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                Db.AddText(cmd, "@Kind", kind, 10);
                Db.AddText(cmd, "@Recipient", recipient, 254);
                Db.AddText(cmd, "@Status", status, 10);
                Db.AddText(cmd, "@Detail", detail, 400);
                Db.AddBit(cmd, "@IsResend", isResend);
                Db.AddInt(cmd, "@AttemptedByUserId", attemptedByUserId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>A job's email attempts, oldest first.</summary>
        public static List<NotificationLogEntry> GetByJob(int jobId)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_NotificationLog_GetByJob"))
            {
                Db.AddInt(cmd, "@JobId", jobId);
                var list = new List<NotificationLogEntry>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new NotificationLogEntry
                        {
                            NotificationId = Db.GetInt(r, "NotificationId"),
                            JobId = Db.GetInt(r, "JobId"),
                            Kind = Db.GetText(r, "Kind"),
                            Recipient = Db.GetText(r, "Recipient"),
                            Status = Db.GetText(r, "Status"),
                            Detail = Db.GetText(r, "Detail"),
                            IsResend = Db.GetBool(r, "IsResend"),
                            AttemptedAt = Db.GetDateTime(r, "AttemptedAt"),
                            AttemptedByUserId = Db.GetInt(r, "AttemptedByUserId"),
                            AttemptedBy = Db.GetText(r, "AttemptedBy"),
                        });
                    }
                }
                return list;
            }
        }
    }
}

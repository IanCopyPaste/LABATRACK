using System;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The Refunds table, through its stored procedures. The rules (one refund per job, handed over
    /// once, the refund code and its limit of wrong tries) are in RefundService. Every method here is
    /// a step of a larger save, so the connection and the transaction come from the caller.
    /// </summary>
    public static class RefundRepository
    {
        /// <summary>Adds the refund of a job being voided, as Pending. Returns the new RefundId.</summary>
        public static int Insert(SqlConnection conn, SqlTransaction tx, int jobId, decimal amount,
                                 byte[] codeHash, byte[] codeSalt, int createdByUserId)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_Insert", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add(new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = amount });
                cmd.Parameters.Add("@CodeHash", SqlDbType.VarBinary, 32).Value = (object)codeHash ?? DBNull.Value;
                cmd.Parameters.Add("@CodeSalt", SqlDbType.VarBinary, 16).Value = (object)codeSalt ?? DBNull.Value;
                cmd.Parameters.Add("@CreatedByUserId", SqlDbType.Int).Value = createdByUserId;

                return Convert.ToInt32(cmd.ExecuteScalar());   // the new RefundId
            }
        }

        /// <summary>
        /// The job's refund, with its row locked until the transaction ends, or null if the job has
        /// none. Two people handing the same refund over at the same moment take turns.
        /// </summary>
        public static Refund GetForUpdate(SqlConnection conn, SqlTransaction tx, int jobId)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_GetForUpdate", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;

                    Refund refund = new Refund();
                    refund.RefundId = (int)r["RefundId"];
                    refund.JobId = (int)r["JobId"];
                    refund.Amount = (decimal)r["Amount"];
                    refund.Status = (string)r["Status"];
                    refund.CodeHash = r["CodeHash"] as byte[];
                    refund.CodeSalt = r["CodeSalt"] as byte[];
                    refund.FailedAttempts = (int)r["FailedAttempts"];
                    refund.CreatedAt = (DateTime)r["CreatedAt"];
                    refund.ReleasedAt = r["ReleasedAt"] as DateTime?;
                    refund.ReleasedBy = r["ReleasedBy"] as string;
                    refund.ReleasedHow = r["ReleasedHow"] as string;
                    return refund;
                }
            }
        }

        /// <summary>
        /// Marks the refund Released, only if it is still Pending. Returns false if someone released it
        /// first: nothing changed, and the caller must not hand any money over.
        /// </summary>
        public static bool Release(SqlConnection conn, SqlTransaction tx, int refundId, int releasedByUserId,
                                   string releasedHow, string overrideReason)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_Release", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RefundId", SqlDbType.Int).Value = refundId;
                cmd.Parameters.Add("@ReleasedByUserId", SqlDbType.Int).Value = releasedByUserId;
                cmd.Parameters.Add("@ReleasedHow", SqlDbType.NVarChar, 16).Value = releasedHow;
                cmd.Parameters.Add("@OverrideReason", SqlDbType.NVarChar, 200).Value = (object)overrideReason ?? DBNull.Value;

                // The procedure ends with SELECT @@ROWCOUNT: 1 = released now, 0 = it was not Pending any more.
                int rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                return rowsAffected == 1;
            }
        }

        /// <summary>Counts one more wrong code. Returns how many wrong tries the refund has now.</summary>
        public static int AddFailedAttempt(SqlConnection conn, SqlTransaction tx, int refundId)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_AddFailedAttempt", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RefundId", SqlDbType.Int).Value = refundId;

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>Saves a new code for a Pending refund (the old one stops working). Returns false if it is not Pending.</summary>
        public static bool SetCode(SqlConnection conn, SqlTransaction tx, int refundId, byte[] codeHash, byte[] codeSalt)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Refunds_SetCode", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RefundId", SqlDbType.Int).Value = refundId;
                cmd.Parameters.Add("@CodeHash", SqlDbType.VarBinary, 32).Value = codeHash;
                cmd.Parameters.Add("@CodeSalt", SqlDbType.VarBinary, 16).Value = codeSalt;

                int rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                return rowsAffected == 1;
            }
        }
    }
}

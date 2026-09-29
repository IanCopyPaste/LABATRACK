using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The Users table, through its stored procedures. The rules are in AuthService.
    /// Each method writes its own connection, command, parameters and reader out in full.
    /// </summary>
    public static class UserRepository
    {
        public static UserAccount GetByUsername(string username)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetByUsername", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return Map(r);
                }
            }
        }

        public static UserAccount GetById(int userId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return Map(r);
                }
            }
        }

        /// <summary>Every account, the owner first, each with HasHistory filled in.</summary>
        public static List<UserAccount> GetAll()
        {
            List<UserAccount> list = new List<UserAccount>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        UserAccount user = Map(r);
                        user.HasHistory = Convert.ToBoolean(r["HasHistory"]);
                        list.Add(user);
                    }
                }
            }
            return list;
        }

        public static UserAccount GetByResetTokenHash(byte[] tokenHash)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_GetByResetTokenHash", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ResetTokenHash", SqlDbType.VarBinary, 32).Value = tokenHash;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return Map(r);
                }
            }
        }

        public static int Insert(string username, byte[] hash, byte[] salt, string role, bool isActive)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_Insert", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;
                cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = hash;
                cmd.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = salt;
                cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 10).Value = role;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());   // the new UserId
            }
        }

        /// <summary>Saves a staff account's username and Active flag. Returns false if it is not a staff account.</summary>
        public static bool UpdateStaff(int userId, string username, bool isActive)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_UpdateStaff", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;

                conn.Open();
                // The procedure ends with SELECT @@ROWCOUNT, read with ExecuteScalar (ExecuteNonQuery
                // returns -1 under SET NOCOUNT ON).
                int rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                return rowsAffected == 1;
            }
        }

        /// <summary>
        /// Deletes a staff account that has no history at all. Returns false, and deletes nothing,
        /// if the account has anything on record (the procedure checks in the same statement).
        /// </summary>
        public static bool DeleteUnused(int userId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_DeleteUnused", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                conn.Open();
                int rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                return rowsAffected == 1;
            }
        }

        /// <summary>Saves a new hash and salt, and clears failed tries, any lock and any reset link.</summary>
        public static void SetPassword(int userId, byte[] hash, byte[] salt)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_SetPassword", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = hash;
                cmd.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = salt;

                conn.Open();
                cmd.ExecuteScalar();
            }
        }

        public static void SetLoginState(int userId, int failedLoginCount, DateTime? lockedUntil)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_SetLoginState", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@FailedLoginCount", SqlDbType.Int).Value = failedLoginCount;
                cmd.Parameters.Add("@LockedUntil", SqlDbType.DateTime2).Value = (object)lockedUntil ?? DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void SetRecoveryEmail(int userId, string email)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_SetRecoveryEmail", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@RecoveryEmail", SqlDbType.NVarChar, 254).Value = (object)email ?? DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void SetResetToken(int userId, byte[] tokenHash, DateTime expiresAt)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Users_SetResetToken", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@ResetTokenHash", SqlDbType.VarBinary, 32).Value = tokenHash;
                cmd.Parameters.Add("@ResetTokenExpiresAt", SqlDbType.DateTime2).Value = expiresAt;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>One row of the Users table. A column that can be empty in SQL is read with "as".</summary>
        private static UserAccount Map(SqlDataReader r)
        {
            UserAccount user = new UserAccount();
            user.UserId = (int)r["UserId"];
            user.Username = (string)r["Username"];
            user.PasswordHash = (byte[])r["PasswordHash"];
            user.PasswordSalt = (byte[])r["PasswordSalt"];
            user.Role = (string)r["Role"];
            user.IsActive = (bool)r["IsActive"];
            user.FailedLoginCount = (int)r["FailedLoginCount"];
            user.LockedUntil = r["LockedUntil"] as DateTime?;
            user.RecoveryEmail = r["RecoveryEmail"] as string;
            user.ResetTokenHash = r["ResetTokenHash"] as byte[];
            user.ResetTokenExpiresAt = r["ResetTokenExpiresAt"] as DateTime?;
            user.CreatedAt = (DateTime)r["CreatedAt"];
            return user;
        }
    }
}

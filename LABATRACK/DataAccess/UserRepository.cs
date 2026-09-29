using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>The Users table, through its stored procedures. The rules are in AuthService.</summary>
    public static class UserRepository
    {
        public static UserAccount GetByUsername(string username)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_GetByUsername"))
            {
                Db.AddText(cmd, "@Username", username, 50);
                return ReadOne(cmd);
            }
        }

        public static UserAccount GetById(int userId)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_GetById"))
            {
                Db.AddInt(cmd, "@UserId", userId);
                return ReadOne(cmd);
            }
        }

        /// <summary>Every account, the owner first, each with HasHistory filled in.</summary>
        public static List<UserAccount> GetAll()
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_GetAll"))
            {
                var list = new List<UserAccount>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        UserAccount user = Map(r);
                        user.HasHistory = Db.GetBool(r, "HasHistory");
                        list.Add(user);
                    }
                }
                return list;
            }
        }

        public static UserAccount GetByResetTokenHash(byte[] tokenHash)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_GetByResetTokenHash"))
            {
                Db.AddBinary(cmd, "@ResetTokenHash", tokenHash, 32);
                return ReadOne(cmd);
            }
        }

        public static int Insert(string username, byte[] hash, byte[] salt, string role, bool isActive)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_Insert"))
            {
                Db.AddText(cmd, "@Username", username, 50);
                Db.AddBinary(cmd, "@PasswordHash", hash, 32);
                Db.AddBinary(cmd, "@PasswordSalt", salt, 16);
                Db.AddText(cmd, "@Role", role, 10);
                Db.AddBit(cmd, "@IsActive", isActive);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>Saves a staff account's username and Active flag. Returns false if it is not a staff account.</summary>
        public static bool UpdateStaff(int userId, string username, bool isActive)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_UpdateStaff"))
            {
                Db.AddInt(cmd, "@UserId", userId);
                Db.AddText(cmd, "@Username", username, 50);
                Db.AddBit(cmd, "@IsActive", isActive);
                return Db.RowsAffected(cmd) == 1;
            }
        }

        /// <summary>
        /// Deletes a staff account that has no history at all. Returns false, and deletes nothing,
        /// if the account has anything on record (the procedure checks in the same statement).
        /// </summary>
        public static bool DeleteUnused(int userId)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_DeleteUnused"))
            {
                Db.AddInt(cmd, "@UserId", userId);
                return Db.RowsAffected(cmd) == 1;
            }
        }

        /// <summary>Saves a new hash and salt, and clears failed tries, any lock and any reset link.</summary>
        public static void SetPassword(int userId, byte[] hash, byte[] salt)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_SetPassword"))
            {
                Db.AddInt(cmd, "@UserId", userId);
                Db.AddBinary(cmd, "@PasswordHash", hash, 32);
                Db.AddBinary(cmd, "@PasswordSalt", salt, 16);
                Db.RowsAffected(cmd);
            }
        }

        public static void SetLoginState(int userId, int failedLoginCount, DateTime? lockedUntil)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_SetLoginState"))
            {
                Db.AddInt(cmd, "@UserId", userId);
                Db.AddInt(cmd, "@FailedLoginCount", failedLoginCount);
                Db.AddDateTime(cmd, "@LockedUntil", lockedUntil);
                cmd.ExecuteNonQuery();
            }
        }

        public static void SetRecoveryEmail(int userId, string email)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_SetRecoveryEmail"))
            {
                Db.AddInt(cmd, "@UserId", userId);
                Db.AddText(cmd, "@RecoveryEmail", email, 254);
                cmd.ExecuteNonQuery();
            }
        }

        public static void SetResetToken(int userId, byte[] tokenHash, DateTime expiresAt)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Users_SetResetToken"))
            {
                Db.AddInt(cmd, "@UserId", userId);
                Db.AddBinary(cmd, "@ResetTokenHash", tokenHash, 32);
                Db.AddDateTime(cmd, "@ResetTokenExpiresAt", expiresAt);
                cmd.ExecuteNonQuery();
            }
        }

        private static UserAccount ReadOne(SqlCommand cmd)
        {
            using (var r = cmd.ExecuteReader())
            {
                return r.Read() ? Map(r) : null;
            }
        }

        private static UserAccount Map(IDataRecord r)
        {
            return new UserAccount
            {
                UserId = Db.GetInt(r, "UserId"),
                Username = Db.GetText(r, "Username"),
                PasswordHash = Db.GetBytes(r, "PasswordHash"),
                PasswordSalt = Db.GetBytes(r, "PasswordSalt"),
                Role = Db.GetText(r, "Role"),
                IsActive = Db.GetBool(r, "IsActive"),
                FailedLoginCount = Db.GetInt(r, "FailedLoginCount"),
                LockedUntil = Db.GetDateTimeOrNull(r, "LockedUntil"),
                RecoveryEmail = Db.GetText(r, "RecoveryEmail"),
                ResetTokenHash = Db.GetBytes(r, "ResetTokenHash"),
                ResetTokenExpiresAt = Db.GetDateTimeOrNull(r, "ResetTokenExpiresAt"),
                CreatedAt = Db.GetDateTime(r, "CreatedAt"),
            };
        }
    }
}

using System;

namespace LABATRACK.Models
{
    /// <summary>
    /// One row of the Users table: an owner or staff account. (Named UserAccount, not User,
    /// because every page already has a property called User.)
    /// </summary>
    public class UserAccount
    {
        public const string OwnerRole = "Owner";
        public const string StaffRole = "Staff";

        public int UserId { get; set; }
        public string Username { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockedUntil { get; set; }
        public string RecoveryEmail { get; set; }
        public byte[] ResetTokenHash { get; set; }
        public DateTime? ResetTokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Filled only by the Staff page's list: the account has something on record (a job, a status
        /// change, a payment or an email), so it can be made inactive but never deleted.
        /// </summary>
        public bool HasHistory { get; set; }

        public bool IsOwner
        {
            get { return Role == OwnerRole; }
        }

        /// <summary>Locked after too many wrong passwords, until LockedUntil passes.</summary>
        public bool IsLockedAt(DateTime now)
        {
            return LockedUntil.HasValue && LockedUntil.Value > now;
        }
    }
}

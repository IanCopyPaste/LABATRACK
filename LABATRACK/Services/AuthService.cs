using System;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Security;
using LABATRACK.DataAccess;
using LABATRACK.Models;
using LABATRACK.Security;

namespace LABATRACK.Services
{
    /// <summary>
    /// Signing in and everything about accounts and passwords, for both login pages
    /// (Login.aspx for staff, Admin/Login.aspx for the owner), the Staff page and Settings.
    ///
    /// The rules it keeps:
    ///   1. A failed sign-in always gets the same answer ("Invalid username or password"),
    ///      whether the username is unknown, the password is wrong, the account is disabled or
    ///      locked, or a staff account tried the owner's login.
    ///   2. Five wrong passwords in a row lock the account for five minutes.
    ///   3. Passwords are stored only as a salted PBKDF2 hash (Security/PasswordHasher.cs).
    ///   4. Only the owner can reset a forgotten password by email. The link holds 32 random
    ///      bytes; only their SHA-256 hash is stored, it works once, and only for 30 minutes.
    /// </summary>
    public static class AuthService
    {
        public const string InvalidSignIn = "Invalid username or password";
        public const int MaxFailedLogins = 5;
        public const int LockMinutes = 5;
        public const int MinPasswordLength = 8;
        public const int ResetLinkMinutes = 30;

        private const string CurrentUserKey = "LabaTrack.CurrentUser";

        // ------------------------------------------------------------ Signing in and out

        /// <summary>
        /// Checks a sign-in on the login page for <paramref name="role"/> (Owner or Staff).
        /// On success it signs the person in (Forms authentication cookie) and returns the
        /// account; on any failure it returns null and the page shows InvalidSignIn.
        /// </summary>
        public static UserAccount SignIn(string username, string password, string role)
        {
            DateTime now = DateTime.Now;
            UserAccount user = string.IsNullOrEmpty(password) ? null : UserRepository.GetByUsername(NormalizeUsername(username));

            // Rule 1: unknown, disabled or locked all fail the same way. The password is still
            // hashed, so even the time the answer takes does not show which one it was.
            if (user == null || !user.IsActive || user.IsLockedAt(now))
            {
                PasswordHasher.Hash(password ?? "", DummySalt);
                return null;
            }

            if (!PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash))
            {
                RecordFailedLogin(user, now);   // rule 2
                return null;
            }

            // The right password on the wrong login page (a staff account on the owner's page, or
            // the owner on the staff page): the same message, but not counted as a wrong password.
            if (user.Role != role)
                return null;

            // A good sign-in clears earlier wrong tries.
            if (user.FailedLoginCount != 0 || user.LockedUntil.HasValue)
                UserRepository.SetLoginState(user.UserId, 0, null);

            // The cookie carries only the username. Who that is (role, still active) is read from
            // the database on every request (CurrentUser and LabaTrackRoleProvider).
            FormsAuthentication.SetAuthCookie(user.Username, false);
            return user;
        }

        /// <summary>
        /// Rule 2: one more wrong password. The fifth in a row locks the account for LockMinutes.
        /// A lock that has already run out starts the count again from one.
        /// </summary>
        private static void RecordFailedLogin(UserAccount user, DateTime now)
        {
            bool lockExpired = user.LockedUntil.HasValue && user.LockedUntil.Value <= now;
            int failed = lockExpired ? 1 : user.FailedLoginCount + 1;
            DateTime? lockedUntil = failed >= MaxFailedLogins ? now.AddMinutes(LockMinutes) : (DateTime?)null;
            UserRepository.SetLoginState(user.UserId, failed, lockedUntil);
        }

        public static void SignOut()
        {
            FormsAuthentication.SignOut();
        }

        /// <summary>
        /// The account signed in on this request, or null when nobody is signed in or the account
        /// has been disabled since (a disabled account is shut out on its next click). Read once
        /// per request from the database and kept in HttpContext.Items for the rest of it.
        /// </summary>
        public static UserAccount CurrentUser
        {
            get
            {
                HttpContext ctx = HttpContext.Current;
                if (ctx == null || ctx.User == null || !ctx.User.Identity.IsAuthenticated) return null;
                if (ctx.Items.Contains(CurrentUserKey)) return ctx.Items[CurrentUserKey] as UserAccount;

                UserAccount user = UserRepository.GetByUsername(ctx.User.Identity.Name);
                if (user != null && !user.IsActive) user = null;
                ctx.Items[CurrentUserKey] = user;
                return user;
            }
        }

        /// <summary>The role of an active account, or null. Used by LabaTrackRoleProvider for Web.config.</summary>
        public static string RoleOf(string username)
        {
            HttpContext ctx = HttpContext.Current;
            UserAccount user = ctx != null && ctx.User != null && ctx.User.Identity.IsAuthenticated
                               && string.Equals(ctx.User.Identity.Name, username, StringComparison.OrdinalIgnoreCase)
                ? CurrentUser
                : UserRepository.GetByUsername(NormalizeUsername(username));
            return user != null && user.IsActive ? user.Role : null;
        }

        // ------------------------------------------------------------ Passwords

        /// <summary>What is wrong with a proposed password, or null if it is acceptable.</summary>
        public static string PasswordProblem(string password)
        {
            if (password == null || password.Length < MinPasswordLength)
                return "Use at least " + MinPasswordLength + " characters.";
            if (password.Length > 100)
                return "Use at most 100 characters.";
            return null;
        }

        /// <summary>
        /// The owner changes their own password on Settings. The current password is checked
        /// first, so nobody can take the account over from a screen left signed in; a wrong one
        /// counts as a failed sign-in, so the five-try lock still applies. The owner stays signed in.
        /// Throws BusinessRuleException with a message for the "current password" box.
        /// </summary>
        public static void ChangeOwnPassword(int userId, string currentPassword, string newPassword)
        {
            DateTime now = DateTime.Now;
            UserAccount user = UserRepository.GetById(userId);
            if (user == null || !user.IsActive)
                throw new BusinessRuleException("Your account could not be found. Sign in again.");
            if (user.IsLockedAt(now))
                throw new BusinessRuleException("Too many wrong passwords. Try again after " + user.LockedUntil.Value.ToString("h:mm tt") + ".");

            if (!PasswordHasher.Verify(currentPassword, user.PasswordSalt, user.PasswordHash))
            {
                RecordFailedLogin(user, now);
                throw new BusinessRuleException("Current password is incorrect.");
            }

            string problem = PasswordProblem(newPassword);
            if (problem != null) throw new BusinessRuleException(problem);
            if (newPassword == currentPassword)
                throw new BusinessRuleException("Choose a password different from the current one.");

            SavePassword(user.UserId, newPassword);
        }

        // A new salt every time, so the stored hash changes even if a password is reused later.
        private static void SavePassword(int userId, string password)
        {
            byte[] salt = PasswordHasher.NewSalt();
            UserRepository.SetPassword(userId, PasswordHasher.Hash(password, salt), salt);
        }

        // ------------------------------------------------------------ Staff accounts (owner only)

        /// <summary>Lower case, no spaces around it: "JCruz " and "jcruz" are the same account.</summary>
        public static string NormalizeUsername(string username)
        {
            return (username ?? "").Trim().ToLowerInvariant();
        }

        /// <summary>
        /// What is wrong with a proposed username, or null if it is acceptable.
        /// <paramref name="editingUserId"/> is the account being renamed, which may keep its own name.
        /// </summary>
        public static string UsernameProblem(string username, int? editingUserId = null)
        {
            string u = NormalizeUsername(username);
            if (u.Length < 3 || u.Length > 50)
                return "Use 3 to 50 characters.";
            if (!u.All(ch => (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '.' || ch == '_' || ch == '-'))
                return "Use letters, numbers, dots, dashes or underscores only.";
            UserAccount taken = UserRepository.GetByUsername(u);
            if (taken != null && taken.UserId != editingUserId)
                return "That username is already taken.";
            return null;
        }

        /// <summary>
        /// Creates a staff account (new accounts always get the Staff role). An inactive one can be
        /// made ahead of time for someone who has not started yet; it cannot sign in until it is active.
        /// </summary>
        public static void CreateStaff(string username, string password, bool active)
        {
            string problem = UsernameProblem(username) ?? PasswordProblem(password);
            if (problem != null) throw new BusinessRuleException(problem);

            byte[] salt = PasswordHasher.NewSalt();
            try
            {
                UserRepository.Insert(NormalizeUsername(username), PasswordHasher.Hash(password, salt), salt, UserAccount.StaffRole, active);
            }
            catch (System.Data.SqlClient.SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new BusinessRuleException("That username was just taken.");   // the unique index on Username
            }
        }

        /// <summary>
        /// The owner's changes to a staff account (the Manage window on the Staff page): its username,
        /// whether it can sign in, and optionally a new password. Staff do not change their own
        /// password; the owner sets it here, for example when they forget it (staff have no email
        /// reset). A new password gets a new salt and also lifts a lock after five wrong tries.
        /// A renamed account signs in with the new username from then on.
        /// </summary>
        public static void UpdateStaff(int userId, string username, string newPassword, bool active)
        {
            UserAccount user = UserRepository.GetById(userId);
            if (user == null || user.Role != UserAccount.StaffRole)
                throw new BusinessRuleException("Only staff accounts are managed here. Change your own account on Settings.");

            string problem = UsernameProblem(username, userId);
            if (problem == null && !string.IsNullOrEmpty(newPassword))
                problem = PasswordProblem(newPassword);
            if (problem != null) throw new BusinessRuleException(problem);

            try
            {
                UserRepository.UpdateStaff(userId, NormalizeUsername(username), active);
            }
            catch (System.Data.SqlClient.SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new BusinessRuleException("That username was just taken.");
            }
            if (!string.IsNullOrEmpty(newPassword))
                SavePassword(userId, newPassword);
        }

        /// <summary>
        /// Deletes a staff account, but only one with no history at all (for example one made by
        /// mistake). Every job, status change, payment and email keeps the name of whoever did it,
        /// so an account with anything on record is never deleted: it is made inactive instead,
        /// which stops it signing in. This is the one hard delete in the system.
        /// </summary>
        public static void DeleteStaff(int userId)
        {
            UserAccount user = UserRepository.GetById(userId);
            if (user == null || user.Role != UserAccount.StaffRole)
                throw new BusinessRuleException("Only a staff account can be deleted.");
            if (!UserRepository.DeleteUnused(userId))
                throw new BusinessRuleException(user.Username + " has jobs, payments or history on record, so the account can't be deleted. "
                                                + "Switch it to Inactive instead: it can't sign in, and its name stays on those records.");
        }

        // ------------------------------------------------------------ Owner's forgotten password

        /// <summary>Saves the email address the owner's reset link is sent to (empty removes it).</summary>
        public static void SetRecoveryEmail(int userId, string email)
        {
            email = (email ?? "").Trim();
            if (email != "" && !EmailService.IsValidAddress(email))
                throw new BusinessRuleException("That does not look like an email address.");
            UserRepository.SetRecoveryEmail(userId, email == "" ? null : email);
        }

        /// <summary>
        /// "Forgot password?" on the owner login. Only an active Owner account with a recovery
        /// email gets a link; for anything else nothing happens. Either way it returns quietly and
        /// the page says the same thing, so the form cannot be used to find out which usernames
        /// exist. A newer link replaces an older one. <paramref name="loginUrl"/> is the full
        /// address of Admin/Login.aspx.
        /// </summary>
        public static void RequestOwnerReset(string username, string loginUrl)
        {
            UserAccount user = UserRepository.GetByUsername(NormalizeUsername(username));
            if (user == null || !user.IsActive || !user.IsOwner || string.IsNullOrWhiteSpace(user.RecoveryEmail))
                return;

            var token = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(token);
            }
            UserRepository.SetResetToken(user.UserId, Sha256(token), DateTime.Now.AddMinutes(ResetLinkMinutes));

            string link = loginUrl + "?token=" + HttpServerUtility.UrlTokenEncode(token);
            EmailService.SendOwnerResetLink(user.RecoveryEmail, link);
        }

        /// <summary>The owner account a reset link belongs to, or null if the link is unknown, used or expired.</summary>
        public static UserAccount FindByResetToken(string tokenText)
        {
            byte[] token = DecodeToken(tokenText);
            if (token == null) return null;

            UserAccount user = UserRepository.GetByResetTokenHash(Sha256(token));
            bool valid = user != null && user.IsActive && user.IsOwner
                         && user.ResetTokenExpiresAt.HasValue && user.ResetTokenExpiresAt.Value > DateTime.Now;
            return valid ? user : null;
        }

        /// <summary>
        /// Sets the new password from a reset link. The link is checked again here (not only when
        /// the page opened). Saving clears the link, so it works once, and clears any lock.
        /// Returns false if the link no longer works.
        /// </summary>
        public static bool CompleteOwnerReset(string tokenText, string newPassword)
        {
            UserAccount user = FindByResetToken(tokenText);
            if (user == null) return false;
            string problem = PasswordProblem(newPassword);
            if (problem != null) throw new BusinessRuleException(problem);
            SavePassword(user.UserId, newPassword);
            return true;
        }

        private static byte[] DecodeToken(string tokenText)
        {
            if (string.IsNullOrEmpty(tokenText) || tokenText.Length > 100) return null;
            try
            {
                byte[] token = HttpServerUtility.UrlTokenDecode(tokenText);
                return token != null && token.Length == 32 ? token : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static byte[] Sha256(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(data);
            }
        }

        // Used only to spend the same time on a sign-in for an unknown account as for a real one.
        private static readonly byte[] DummySalt = PasswordHasher.NewSalt();
    }
}

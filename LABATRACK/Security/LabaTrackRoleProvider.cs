using System;
using System.Web.Security;
using LABATRACK.Models;
using LABATRACK.Services;

namespace LABATRACK.Security
{
    /// <summary>
    /// Tells ASP.NET which role a signed-in user has, so the rules in Web.config can use roles:
    /// the Admin folder allows only the Owner role, so a staff account that types an admin
    /// address is turned away before the page even runs. (AdminBasePage checks again.)
    ///
    /// The role is read from the Users table on every request (through AuthService), never from
    /// the cookie, so a disabled account has no role at once. It is registered in Web.config
    /// under &lt;roleManager&gt;. Only the two reading methods are needed; roles are fixed
    /// (Owner and Staff), so the methods for creating or changing roles are not supported.
    /// </summary>
    public class LabaTrackRoleProvider : RoleProvider
    {
        public override string ApplicationName
        {
            get { return "LabaTrack"; }
            set { }
        }

        public override string[] GetRolesForUser(string username)
        {
            string role = AuthService.RoleOf(username);
            return role == null ? new string[0] : new[] { role };
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            return string.Equals(AuthService.RoleOf(username), roleName, StringComparison.Ordinal);
        }

        public override bool RoleExists(string roleName)
        {
            return roleName == UserAccount.OwnerRole || roleName == UserAccount.StaffRole;
        }

        public override string[] GetAllRoles()
        {
            return new[] { UserAccount.OwnerRole, UserAccount.StaffRole };
        }

        // Roles are fixed and accounts are managed on the Staff page, so these are not used.
        public override void CreateRole(string roleName) { throw new NotSupportedException(); }
        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole) { throw new NotSupportedException(); }
        public override void AddUsersToRoles(string[] usernames, string[] roleNames) { throw new NotSupportedException(); }
        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames) { throw new NotSupportedException(); }
        public override string[] GetUsersInRole(string roleName) { throw new NotSupportedException(); }
        public override string[] FindUsersInRole(string roleName, string usernameToMatch) { throw new NotSupportedException(); }
    }
}

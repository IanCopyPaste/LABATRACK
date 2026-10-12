namespace LABATRACK.models
{
    public class UserAccount
    {
        public const string OwnerRole = "Owner";

        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public string RecoveryEmail { get; set; }
        public bool HasHistory { get; set; }

        public bool IsOwner
        {
            get { return Role == OwnerRole; }
        }
    }
}

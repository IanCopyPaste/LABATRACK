using System.Security.Cryptography;

namespace LABATRACK.security
{
    public static class PasswordHasher
    {
        public const int SaltBytes = 16;
        public const int HashBytes = 32;
        public const int Iterations = 100000;

        public static byte[] NewSalt()
        {
            var salt = new byte[SaltBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        public static byte[] Hash(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashBytes);
            }
        }

        public static bool Verify(string password, byte[] salt, byte[] storedHash)
        {
            if (password == null || salt == null || storedHash == null) return false;
            return SameBytes(Hash(password, salt), storedHash);
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}

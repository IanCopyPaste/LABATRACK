using System.Security.Cryptography;

namespace LABATRACK.Security
{
    /// <summary>
    /// Turns a password into a hash that can be stored, and checks a typed password against it.
    /// The password itself is never stored or logged.
    ///
    /// PBKDF2 (Rfc2898DeriveBytes) with SHA-256 runs the password and a salt through 100,000
    /// rounds of hashing, so guessing passwords from a stolen hash is slow. The salt is 16 random
    /// bytes made for each user, so two people with the same password get different hashes.
    /// 02_seed_settings.sql uses these same settings for the demo owner account.
    /// </summary>
    public static class PasswordHasher
    {
        public const int SaltBytes = 16;
        public const int HashBytes = 32;
        public const int Iterations = 100000;

        /// <summary>A new random salt, for a new password.</summary>
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

        /// <summary>True if <paramref name="password"/> is the password that produced <paramref name="storedHash"/>.</summary>
        public static bool Verify(string password, byte[] salt, byte[] storedHash)
        {
            if (password == null || salt == null || storedHash == null) return false;
            return SameBytes(Hash(password, salt), storedHash);
        }

        // Compares every byte even after a difference is found, so the time taken does not
        // give away how much of a guess was right.
        private static bool SameBytes(byte[] a, byte[] b)
        {
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}

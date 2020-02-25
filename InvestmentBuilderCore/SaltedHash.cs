using System;
using System.Security.Cryptography;

namespace InvestmentBuilderCore
{
    public static class SaltedHash
    {
        //public string Hash { get; private set; }
        //public string Salt { get; private set; }

        public static string GenerateSalt()
        {
            var saltBytes = new byte[256];
            using (var provider = new RNGCryptoServiceProvider())
                provider.GetNonZeroBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string GenerateHash(string password, string salt)
        {
            return  Convert.ToBase64String(_ComputeHash(salt,password));
        }

        private static byte[] _ComputeHash(string salt, string password)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 1000))
                return rfc2898DeriveBytes.GetBytes(256);
        }

        public static bool Verify(string salt, string hash, string password)
        {
            return slowEquals(Convert.FromBase64String(hash),
                              _ComputeHash(salt, password));
        }

        private static bool slowEquals(byte[] a, byte[] b)
        {
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        //public SaltedHash(string password)
        //{
        //    var saltBytes = new byte[32];
        //    using (var provider = new RNGCryptoServiceProvider())
        //        provider.GetNonZeroBytes(saltBytes);
        //    Salt = Convert.ToBase64String(saltBytes);
        //    Hash = ComputeHash(Salt, password);
        //}
    }
}

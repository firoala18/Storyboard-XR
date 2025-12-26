using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.Utility
{
    public static class Tokens
    {
        public static string NewSlug(int bytes = 8)
        {
            var raw = RandomNumberGenerator.GetBytes(bytes);
            return ToBase62(raw);
        }

        public static string NewEditKey()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(); // 32 hex chars
        }

        public static string Sha256Hex(string s)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(hash).ToLowerInvariant(); // 64 hex chars
        }

        private static string ToBase62(ReadOnlySpan<byte> bytes)
        {
            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var bi = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
            if (bi == 0) return "0";
            var chars = new Stack<char>();
            while (bi > 0)
            {
                bi = BigInteger.DivRem(bi, 62, out var rem);
                chars.Push(alphabet[(int)rem]);
            }
            return new string(chars.ToArray());
        }
    }
}

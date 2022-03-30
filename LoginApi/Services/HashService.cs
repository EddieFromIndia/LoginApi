using System.Security.Cryptography;
using System.Text;

namespace LoginApi.Services
{
    public class HashService
    {
        /// <summary>
        /// Takes a password as string and hashes it using SHA256 algorithm
        /// </summary>
        /// <param name="password"></param>
        /// <returns>The hashed password as string</returns>
        public static string HashPassword(string password)
        {
            using SHA256? sha256 = SHA256.Create();
            byte[]? hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            string? hashedPassword = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            return hashedPassword;
        }
    }
}
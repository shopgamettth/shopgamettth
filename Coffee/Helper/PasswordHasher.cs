using Microsoft.AspNetCore.Identity;
using Coffee.Models;

namespace Coffee.Helper
{
    public class PasswordHasherHelper
    {
        private readonly PasswordHasher<User> _hasher = new();

        public string Hash(User user, string password)
        {
            return _hasher.HashPassword(user, password);
        }

        public bool Verify(User user, string hashedPassword, string inputPassword)
        {
            var result = _hasher.VerifyHashedPassword(user, hashedPassword, inputPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
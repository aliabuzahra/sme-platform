using System.Security.Cryptography;
using System.Text;

namespace Sme.Identity.Auth;

/// <summary>Pure helpers for one-time codes: generation and hashing. The flow lives in <see cref="OtpService"/>.</summary>
public static class OtpCodes
{
    public static string Generate(int digits)
    {
        if (digits is < 4 or > 10) throw new ArgumentOutOfRangeException(nameof(digits));
        var max = (int)Math.Pow(10, digits);
        return RandomNumberGenerator.GetInt32(0, max).ToString().PadLeft(digits, '0');
    }

    /// <summary>The code is low entropy, so it is hashed with the row id as salt and a server pepper.</summary>
    public static string Hash(Guid id, string code, string pepper)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{id:N}:{code}:{pepper}"));
        return Convert.ToHexStringLower(bytes);
    }

    public static bool Matches(Guid id, string code, string pepper, string hash) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(id, code, pepper)), Encoding.ASCII.GetBytes(hash));
}

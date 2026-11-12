using System.Text.RegularExpressions;

namespace Sme.Identity.Auth;

/// <summary>Phone numbers are stored and compared in E.164 form, for example <c>+962791234567</c>.</summary>
public static partial class PhoneNumber
{
    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164();

    /// <summary>
    /// Accepts E.164 with optional spaces or dashes, and Jordanian local numbers (<c>07xxxxxxxx</c>),
    /// which become <c>+9627xxxxxxxx</c>. Anything else is rejected.
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(input)) return false;
        var digits = new string(input.Where(c => char.IsAsciiDigit(c) || c == '+').ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal)) digits = "+" + digits[2..];
        if (digits.StartsWith("07", StringComparison.Ordinal) && digits.Length == 10) digits = "+962" + digits[1..];
        if (!E164().IsMatch(digits)) return false;
        normalized = digits;
        return true;
    }
}

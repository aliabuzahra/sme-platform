namespace Sme.Identity.Auth;

/// <summary>Bound from the <c>Otp</c> section.</summary>
public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary><c>fake</c> is the only sender until Notifications delivers real SMS (UC-012).</summary>
    public string Sender { get; set; } = "fake";

    public int Digits { get; set; } = 6;

    public int LifetimeSeconds { get; set; } = 5 * 60;

    /// <summary>Wrong guesses allowed before a code is burnt and a new one must be requested.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>A phone may not request more than this many codes within <see cref="RequestWindowSeconds"/>.</summary>
    public int MaxRequestsPerWindow { get; set; } = 3;

    public int RequestWindowSeconds { get; set; } = 10 * 60;
}

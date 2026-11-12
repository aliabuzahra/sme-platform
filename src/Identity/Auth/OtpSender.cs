using System.Collections.Concurrent;

namespace Sme.Identity.Auth;

/// <summary>Delivers a one-time code to a phone. The real sender goes through Notifications (UC-012).</summary>
public interface IOtpSender
{
    Task SendAsync(string phone, string code, CancellationToken ct);
}

/// <summary>
/// Development and test sender: keeps the last code per phone in memory and logs it, so sign-in
/// works end to end without an SMS provider. Selected by <c>Otp:Sender = "fake"</c>.
/// </summary>
public sealed class FakeOtpSender(ILogger<FakeOtpSender> logger) : IOtpSender
{
    private readonly ConcurrentDictionary<string, string> _codes = new();

    public Task SendAsync(string phone, string code, CancellationToken ct)
    {
        _codes[phone] = code;
        logger.LogInformation("OTP for {Phone}: {Code} (fake sender)", phone, code);
        return Task.CompletedTask;
    }

    /// <summary>The last code sent to <paramref name="phone"/>, or null if none was.</summary>
    public string? LastCodeFor(string phone) => _codes.GetValueOrDefault(phone);
}

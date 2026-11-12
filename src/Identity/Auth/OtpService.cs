using Npgsql;
using Sme.BuildingBlocks.Auth;
using Sme.BuildingBlocks.Data;

namespace Sme.Identity.Auth;

public enum OtpVerifyOutcome { Ok, Invalid, Expired, TooManyAttempts }

/// <summary>
/// Requests and checks one-time codes. Codes are platform data (no tenant yet), so this runs on
/// the platform connection. The signing key doubles as the hash pepper: both are server secrets.
/// </summary>
public sealed class OtpService(TenantConnectionFactory connections, IOtpSender sender, OtpOptions options, JwtOptions jwt, TimeProvider clock)
{
    public int LifetimeSeconds => options.LifetimeSeconds;

    /// <summary>
    /// Issues a code when the phone is allowed one. Returns false when the phone asked too often;
    /// callers still answer 202 so that a request cannot tell registered phones from unknown ones.
    /// </summary>
    public async Task<bool> RequestAsync(string phone, CancellationToken ct)
    {
        await using var connection = await connections.OpenPlatformAsync(ct);
        var now = clock.GetUtcNow();

        await using (var recent = new NpgsqlCommand(
            "SELECT count(*) FROM identity.otp_codes WHERE phone = @phone AND created_at > @since", connection))
        {
            recent.Parameters.AddWithValue("phone", phone);
            recent.Parameters.AddWithValue("since", now.AddSeconds(-options.RequestWindowSeconds));
            if ((long)(await recent.ExecuteScalarAsync(ct))! >= options.MaxRequestsPerWindow) return false;
        }

        // A new code retires the earlier ones for the phone.
        await using (var retire = new NpgsqlCommand(
            "UPDATE identity.otp_codes SET consumed_at = @now WHERE phone = @phone AND consumed_at IS NULL", connection))
        {
            retire.Parameters.AddWithValue("phone", phone);
            retire.Parameters.AddWithValue("now", now);
            await retire.ExecuteNonQueryAsync(ct);
        }

        var id = Guid.NewGuid();
        var code = OtpCodes.Generate(options.Digits);
        await using (var insert = new NpgsqlCommand(
            "INSERT INTO identity.otp_codes (id, phone, code_hash, expires_at, created_at) VALUES (@id, @phone, @hash, @expires, @now)", connection))
        {
            insert.Parameters.AddWithValue("id", id);
            insert.Parameters.AddWithValue("phone", phone);
            insert.Parameters.AddWithValue("hash", OtpCodes.Hash(id, code, jwt.SigningKey));
            insert.Parameters.AddWithValue("expires", now.AddSeconds(options.LifetimeSeconds));
            insert.Parameters.AddWithValue("now", now);
            await insert.ExecuteNonQueryAsync(ct);
        }

        await sender.SendAsync(phone, code, ct);
        return true;
    }

    /// <summary>Checks the latest live code for the phone. A correct code is consumed; a wrong one costs an attempt.</summary>
    public async Task<OtpVerifyOutcome> VerifyAsync(string phone, string code, CancellationToken ct)
    {
        await using var connection = await connections.OpenPlatformAsync(ct);
        var now = clock.GetUtcNow();

        Guid id;
        string hash;
        DateTimeOffset expiresAt;
        int attempts;
        await using (var latest = new NpgsqlCommand(
            "SELECT id, code_hash, expires_at, attempts FROM identity.otp_codes " +
            "WHERE phone = @phone AND consumed_at IS NULL ORDER BY created_at DESC LIMIT 1", connection))
        {
            latest.Parameters.AddWithValue("phone", phone);
            await using var reader = await latest.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return OtpVerifyOutcome.Invalid;
            id = reader.GetGuid(0);
            hash = reader.GetString(1);
            expiresAt = reader.GetFieldValue<DateTime>(2);
            attempts = reader.GetInt32(3);
        }

        if (expiresAt <= now) return OtpVerifyOutcome.Expired;
        if (attempts >= options.MaxAttempts) return OtpVerifyOutcome.TooManyAttempts;

        if (!OtpCodes.Matches(id, code, jwt.SigningKey, hash))
        {
            await using var fail = new NpgsqlCommand("UPDATE identity.otp_codes SET attempts = attempts + 1 WHERE id = @id", connection);
            fail.Parameters.AddWithValue("id", id);
            await fail.ExecuteNonQueryAsync(ct);
            return attempts + 1 >= options.MaxAttempts ? OtpVerifyOutcome.TooManyAttempts : OtpVerifyOutcome.Invalid;
        }

        await using var consume = new NpgsqlCommand(
            "UPDATE identity.otp_codes SET consumed_at = @now WHERE id = @id AND consumed_at IS NULL", connection);
        consume.Parameters.AddWithValue("id", id);
        consume.Parameters.AddWithValue("now", now);
        return await consume.ExecuteNonQueryAsync(ct) == 1 ? OtpVerifyOutcome.Ok : OtpVerifyOutcome.Invalid;
    }
}

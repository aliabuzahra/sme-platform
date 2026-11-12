using Npgsql;
using Sme.BuildingBlocks.Data;

namespace Sme.Identity.Auth;

/// <summary>A user as seen at sign-in time, with the tenant's name and module entitlements.</summary>
public sealed record SignInCandidate(Guid UserId, Guid TenantId, string TenantName, string Phone, string Name, string Role, string[] Modules);

public sealed class UserRepository(TenantConnectionFactory connections)
{
    /// <summary>Every tenant membership for a phone. Sign-in happens before a tenant is known, so this is platform data.</summary>
    public async Task<IReadOnlyList<SignInCandidate>> FindByPhoneAsync(string phone, CancellationToken ct)
    {
        await using var connection = await connections.OpenPlatformAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "SELECT u.id, u.tenant_id, t.name, u.phone, u.name, u.role, t.modules " +
            "FROM identity.users u JOIN identity.tenants t ON t.id = u.tenant_id " +
            "WHERE u.phone = @phone AND t.status = 'active' ORDER BY u.created_at", connection);
        cmd.Parameters.AddWithValue("phone", phone);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var users = new List<SignInCandidate>();
        while (await reader.ReadAsync(ct))
        {
            users.Add(new SignInCandidate(
                reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetFieldValue<string[]>(6)));
        }
        return users;
    }
}

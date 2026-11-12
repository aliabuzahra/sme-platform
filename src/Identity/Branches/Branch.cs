namespace Sme.Identity.Branches;

public sealed record Branch(
    Guid Id,
    Guid TenantId,
    string Name,
    Dictionary<string, List<OpenInterval>> Hours,
    DateTimeOffset CreatedAt);

public sealed record CreateBranchRequest(string Name, Dictionary<string, List<OpenInterval>>? Hours);
